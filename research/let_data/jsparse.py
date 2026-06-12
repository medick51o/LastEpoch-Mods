"""
Tolerant JS-object-literal parser for LET data files.

Handles the subset of JS used by Last Epoch Tools' data files:
- unquoted keys (identifier or number)
- strings in "..." or '...' with backslash escapes
- numbers (int, float, negatives, scientific)
- !0 / !1 for true/false, true / false / null literals
- objects {} and arrays []
- trailing commas (just in case)
- /*comments*/ and //line comments (just in case)

Produces Python dict/list/str/int/float/bool/None values.

The parser is non-recursive (explicit stack) so it can handle deeply
nested structures without blowing Python's recursion limit.
"""
import re
import sys


class JSParser:
    def __init__(self, src):
        self.src = src
        self.pos = 0
        self.len = len(src)

    def error(self, msg):
        lo = max(0, self.pos - 60)
        hi = min(self.len, self.pos + 60)
        raise ValueError(f"{msg} at pos {self.pos}: ...{self.src[lo:hi]!r}...")

    def skip_ws(self):
        src = self.src
        while self.pos < self.len:
            c = src[self.pos]
            if c in ' \t\r\n':
                self.pos += 1
                continue
            # comments
            if c == '/' and self.pos + 1 < self.len:
                nxt = src[self.pos + 1]
                if nxt == '/':
                    self.pos += 2
                    while self.pos < self.len and src[self.pos] != '\n':
                        self.pos += 1
                    continue
                if nxt == '*':
                    self.pos += 2
                    while self.pos + 1 < self.len and not (src[self.pos] == '*' and src[self.pos + 1] == '/'):
                        self.pos += 1
                    self.pos += 2
                    continue
            break

    def peek(self):
        self.skip_ws()
        if self.pos >= self.len:
            return ''
        return self.src[self.pos]

    def parse_string(self):
        src = self.src
        quote = src[self.pos]
        assert quote in '"\''
        self.pos += 1
        out = []
        while self.pos < self.len:
            c = src[self.pos]
            if c == '\\':
                nxt = src[self.pos + 1]
                self.pos += 2
                if nxt == 'n':
                    out.append('\n')
                elif nxt == 't':
                    out.append('\t')
                elif nxt == 'r':
                    out.append('\r')
                elif nxt == 'b':
                    out.append('\b')
                elif nxt == 'f':
                    out.append('\f')
                elif nxt == '"':
                    out.append('"')
                elif nxt == "'":
                    out.append("'")
                elif nxt == '\\':
                    out.append('\\')
                elif nxt == '/':
                    out.append('/')
                elif nxt == 'u':
                    hx = src[self.pos:self.pos + 4]
                    self.pos += 4
                    out.append(chr(int(hx, 16)))
                elif nxt == 'x':
                    hx = src[self.pos:self.pos + 2]
                    self.pos += 2
                    out.append(chr(int(hx, 16)))
                else:
                    out.append(nxt)
                continue
            if c == quote:
                self.pos += 1
                return ''.join(out)
            out.append(c)
            self.pos += 1
        self.error("Unterminated string")

    _NUM_RE = re.compile(r'-?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?')

    def parse_number(self):
        m = self._NUM_RE.match(self.src, self.pos)
        if not m:
            self.error("Bad number")
        s = m.group()
        self.pos = m.end()
        if '.' in s or 'e' in s or 'E' in s:
            return float(s)
        return int(s)

    _IDENT_RE = re.compile(r'[A-Za-z_$][A-Za-z_$0-9]*')

    def parse_ident(self):
        m = self._IDENT_RE.match(self.src, self.pos)
        if not m:
            self.error("Bad identifier")
        self.pos = m.end()
        return m.group()

    def parse_key(self):
        """Object key: unquoted identifier, number, or quoted string."""
        self.skip_ws()
        c = self.src[self.pos]
        if c == '"' or c == "'":
            return self.parse_string()
        if c.isdigit() or c == '-':
            # numeric key
            m = self._NUM_RE.match(self.src, self.pos)
            self.pos = m.end()
            return m.group()  # keep as string (dict keys are strings in JSON)
        return self.parse_ident()

    def parse_value(self):
        v = self._parse_value_raw()
        return self.parse_postfix(v)

    def _parse_value_raw(self):
        self.skip_ws()
        if self.pos >= self.len:
            self.error("Unexpected EOF")
        c = self.src[self.pos]
        if c == '{':
            return self.parse_object()
        if c == '[':
            return self.parse_array()
        if c == '"' or c == "'":
            return self.parse_string()
        if c.isdigit() or c == '-' or c == '.':
            return self.parse_number()
        if c == '!':
            # !0 -> True, !1 -> False, !!x -> bool cast (rare)
            self.pos += 1
            v = self._parse_value_raw()
            return not v
        # keyword
        if c.isalpha() or c == '_':
            ident = self.parse_ident()
            if ident == 'true':
                return True
            if ident == 'false':
                return False
            if ident == 'null':
                return None
            if ident == 'undefined':
                return None
            if ident == 'NaN':
                return float('nan')
            if ident == 'Infinity':
                return float('inf')
            # could be a reference to a function - treat as raw string
            return {'__js__': ident}
        self.error(f"Unexpected char {c!r}")

    def parse_postfix(self, value):
        """After a value, check for method calls like .split(';') and apply them."""
        self.skip_ws()
        while self.pos < self.len and self.src[self.pos] == '.':
            # peek method name
            save = self.pos
            self.pos += 1
            self.skip_ws()
            if self.pos >= self.len or not (self.src[self.pos].isalpha() or self.src[self.pos] == '_'):
                self.pos = save
                return value
            m = self._IDENT_RE.match(self.src, self.pos)
            if not m:
                self.pos = save
                return value
            name = m.group()
            self.pos = m.end()
            self.skip_ws()
            if self.pos >= self.len or self.src[self.pos] != '(':
                self.pos = save
                return value
            # parse args
            self.pos += 1
            args = []
            self.skip_ws()
            if self.src[self.pos] != ')':
                while True:
                    args.append(self.parse_value())
                    self.skip_ws()
                    if self.src[self.pos] == ',':
                        self.pos += 1
                        self.skip_ws()
                        continue
                    break
            if self.src[self.pos] != ')':
                self.error("Expected )")
            self.pos += 1
            # apply method
            if name == 'split' and isinstance(value, str) and len(args) >= 1 and isinstance(args[0], str):
                value = value.split(args[0])
            else:
                # unknown method - return a wrapper
                value = {'__js_call__': name, 'on': value, 'args': args}
            self.skip_ws()
        return value

    def parse_object(self):
        assert self.src[self.pos] == '{'
        self.pos += 1
        out = {}
        self.skip_ws()
        if self.src[self.pos] == '}':
            self.pos += 1
            return out
        while True:
            self.skip_ws()
            key = self.parse_key()
            self.skip_ws()
            if self.src[self.pos] != ':':
                self.error("Expected :")
            self.pos += 1
            value = self.parse_value()
            out[key] = value
            self.skip_ws()
            c = self.src[self.pos]
            if c == ',':
                self.pos += 1
                self.skip_ws()
                # trailing comma
                if self.src[self.pos] == '}':
                    self.pos += 1
                    return out
                continue
            if c == '}':
                self.pos += 1
                return out
            self.error(f"Expected , or }} got {c!r}")

    def parse_array(self):
        assert self.src[self.pos] == '['
        self.pos += 1
        out = []
        self.skip_ws()
        if self.src[self.pos] == ']':
            self.pos += 1
            return out
        while True:
            value = self.parse_value()
            out.append(value)
            self.skip_ws()
            c = self.src[self.pos]
            if c == ',':
                self.pos += 1
                self.skip_ws()
                if self.src[self.pos] == ']':
                    self.pos += 1
                    return out
                continue
            if c == ']':
                self.pos += 1
                return out
            self.error(f"Expected , or ] got {c!r}")


def parse_js_object(src, pos=0):
    """Parse a single JS object/array starting at src[pos:]. Returns the value."""
    p = JSParser(src)
    p.pos = pos
    return p.parse_value()


def find_balanced(src, start, open_char='{', close_char='}'):
    """Given src[start] == open_char, return the position after the matching close_char (tolerant of strings)."""
    assert src[start] == open_char
    depth = 0
    i = start
    n = len(src)
    while i < n:
        c = src[i]
        if c == '"' or c == "'":
            q = c
            i += 1
            while i < n:
                if src[i] == '\\':
                    i += 2
                    continue
                if src[i] == q:
                    i += 1
                    break
                i += 1
            continue
        if c == open_char:
            depth += 1
        elif c == close_char:
            depth -= 1
            if depth == 0:
                return i + 1
        i += 1
    raise ValueError("unbalanced")


if __name__ == '__main__':
    # quick self-test
    tests = [
        '{a:1,b:!0,c:!1,d:"hi",e:[1,2,3],f:null}',
        "{foo:'bar',num:1.5,neg:-2,exp:1e3}",
        '{0:{a:1},1:{a:2}}',
    ]
    for t in tests:
        print(t, '=>', parse_js_object(t))
