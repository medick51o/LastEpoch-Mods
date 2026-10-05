internal static partial class BorderProbe {
    internal static void OnLateUpdate()
    {
        try
        {
            if (!GateEnabled())
            {
                HandleGateOff();
                return;
            }

            if (s_latchedOff) return;
            s_runtimeActive = true;
            if (s_ownedBorderCount == 0) return;

            s_deadTooltipKeys.Clear();
            foreach (var pair in s_tooltips)
            {
                TooltipState tooltipState = pair.Value;
                if (tooltipState.Tooltip == null)
                {
                    DestroyRows(tooltipState);
                    s_deadTooltipKeys.Add(pair.Key);
                    continue;
                }

                for (int i = tooltipState.Rows.Count - 1; i >= 0; i--)
                {
                    RowState row = tooltipState.Rows[i];
                    if (row.Tmp == null)
                    {
                        DestroyRow(row);
                        tooltipState.Rows.RemoveAt(i);
                        continue;
                    }

                    string text = row.Tmp.text ?? "";
                    if (!row.Tmp.gameObject.activeInHierarchy || !IsCandidateText(text))
                    {
                        // deliberately leave stale border visible
                        continue;
                    }

                    bool pending = row.ResyncAfterFrame > 0 &&
                                   Time.frameCount >= row.ResyncAfterFrame;
                    if (pending || LayoutChanged(row))
                    {
                        row.ResyncAfterFrame = 0;
                        RefreshRow(row, text, i + 1, scheduleNextFrame: false);
                    }
                }
            }

            foreach (int key in s_deadTooltipKeys) s_tooltips.Remove(key);
            s_deadTooltipKeys.Clear();
        }
        catch (Exception ex) { Fail("LateUpdate", ex); }
    }


    private static int CountUnitBoxes(string richText)
    {
        if (string.IsNullOrEmpty(richText)) return 0;
        int unitLinks = CountLinks(richText, LinkOpen);
        int dividerLinks = CountLinks(richText, DividerLinkOpen);
        return Math.Max(0, unitLinks - dividerLinks);
    }

    private static int CountLinks(string richText, string linkOpen)
    {
        int count = 0;
        int index = 0;
        while ((index = richText.IndexOf(linkOpen, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += linkOpen.Length;
        }
        return count;
    }

    private static bool IsCandidateText(string text)
        => text.EndsWith(Marker, StringComparison.Ordinal) &&
           text.Contains(LinkOpen, StringComparison.Ordinal);


    private static void Hide(RowState row)
    {
        if (row == null) return;
        foreach (UnitState unit in row.Units) Hide(unit);
    }

    private static void Hide(UnitState unit)
    {
        if (unit?.Border != null && unit.Border.activeSelf) unit.Border.SetActive(false);
        HideDivider(unit);
    }

    private static void HideDivider(UnitState unit)
    {
        if (unit?.Divider != null && unit.Divider.activeSelf) unit.Divider.SetActive(false);
    }


}
