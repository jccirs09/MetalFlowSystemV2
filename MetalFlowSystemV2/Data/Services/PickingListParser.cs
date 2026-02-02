using System.Globalization;
using System.Text.RegularExpressions;

namespace MetalFlowSystemV2.Data.Services
{
    public class PickingListImportDto
    {
        public string PickingListNumber { get; set; } = "";
        public DateTime? PrintDate { get; set; }
        public DateTime? ShipDate { get; set; }
        public string Buyer { get; set; } = "";
        public string SalesRep { get; set; } = "";
        public string ShipVia { get; set; } = "";
        public string SoldTo { get; set; } = "";
        public string ShipTo { get; set; } = "";
        public decimal TotalWeightLbs { get; set; }
        public string OrderInstructions { get; set; } = "";
        public List<PickingListLineImportDto> Lines { get; set; } = new();
    }

    public class PickingListLineImportDto
    {
        public int LineNumber { get; set; }
        public string ItemCode { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal OrderQtyValue { get; set; }
        public string OrderQtyUnit { get; set; } = ""; // PCS | LBS
        public decimal WidthIn { get; set; }
        public decimal LengthIn { get; set; }
        public List<ReservedMaterialImportDto> ReservedMaterials { get; set; } = new();
        public string LineInstructions { get; set; } = "";
    }

    public class ReservedMaterialImportDto
    {
        public string TagNumber { get; set; } = "";
        public string MillRef { get; set; } = "";
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "";
        public string Size { get; set; } = "";
        public string Location { get; set; } = "";
    }

    public class PickingListParser
    {
        private enum ParseState
        {
            Header,
            OrderInstructions,
            Lines,
            LineDetails,
            ReservedMaterials,
            LineInstructions
        }

        public PickingListImportDto Parse(string text)
        {
            var dto = new PickingListImportDto();
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            ParseState state = ParseState.Header;
            PickingListLineImportDto currentLine = null;
            ReservedMaterialImportDto currentReserved = null;

            // Buffer for multiline fields
            List<string> instructionBuffer = new();

            void FlushInstructions()
            {
                if (instructionBuffer.Count > 0)
                {
                    var joined = string.Join("\n", instructionBuffer).Trim();
                    if (state == ParseState.OrderInstructions)
                    {
                        dto.OrderInstructions = joined == "—" ? "" : joined;
                    }
                    else if (state == ParseState.LineInstructions && currentLine != null)
                    {
                        currentLine.LineInstructions = joined == "—" ? "" : joined;
                    }
                    instructionBuffer.Clear();
                }
            }

            for (int i = 0; i < lines.Length; i++)
            {
                var rawLine = lines[i];
                var line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("----------------"))
                {
                    // Check for section headers which are often surrounded by dashes
                    // But usually the header name is on its own line.
                    // Let's look ahead/behind or just check the line content if it matches known headers
                    continue;
                }

                // Section Transitions
                if (line == "ORDER_INSTRUCTIONS")
                {
                    FlushInstructions();
                    state = ParseState.OrderInstructions;
                    continue;
                }
                if (line == "LINES")
                {
                    FlushInstructions();
                    state = ParseState.Lines;
                    continue;
                }

                // Header Parsing
                if (state == ParseState.Header)
                {
                    if (line.StartsWith("PICKING_LIST_NO:")) dto.PickingListNumber = ParseString(line);
                    else if (line.StartsWith("PRINT_DATE:")) dto.PrintDate = ParseDateTime(line);
                    else if (line.StartsWith("SHIP_DATE:")) dto.ShipDate = ParseDate(line);
                    else if (line.StartsWith("BUYER:")) dto.Buyer = ParseString(line);
                    else if (line.StartsWith("SALES_REP:")) dto.SalesRep = ParseString(line);
                    else if (line.StartsWith("SHIP_VIA:")) dto.ShipVia = ParseString(line);
                    else if (line.StartsWith("SOLD_TO:")) dto.SoldTo = ParseString(line);
                    else if (line.StartsWith("SHIP_TO:")) dto.ShipTo = ParseString(line);
                    else if (line.StartsWith("TOTAL_WEIGHT_LBS:")) dto.TotalWeightLbs = ParseDecimal(line);
                }
                // Order Instructions Parsing
                else if (state == ParseState.OrderInstructions)
                {
                    // Keep reading until we hit a section delimiter or new key that indicates end
                    // The format shows LINES is next.
                    // We handle LINES transition above.
                    instructionBuffer.Add(rawLine); // Preserve indent? Prompt says "Multiline text".
                }
                // Lines Parsing
                else if (state == ParseState.Lines || state == ParseState.LineDetails || state == ParseState.ReservedMaterials || state == ParseState.LineInstructions)
                {
                    // Detect start of new line
                    if (line.StartsWith("LINE:"))
                    {
                        FlushInstructions(); // Flush line instructions of previous line if any
                        currentLine = new PickingListLineImportDto();
                        currentLine.LineNumber = ParseInt(line);
                        dto.Lines.Add(currentLine);
                        state = ParseState.LineDetails;
                        continue;
                    }

                    if (currentLine == null) continue; // Should not happen if format is valid

                    if (line.StartsWith("ITEM_CODE:")) currentLine.ItemCode = ParseString(line);
                    else if (line.StartsWith("DESCRIPTION:")) currentLine.Description = ParseString(line);
                    else if (line == "ORDER_QTY:")
                    {
                        // Next lines are - VALUE and - UNIT
                        // We can just set state or just parse them as they come since they have unique keys
                        continue;
                    }
                    else if (line.StartsWith("- VALUE:")) currentLine.OrderQtyValue = ParseDecimal(line.Replace("- ", ""));
                    else if (line.StartsWith("- UNIT:") && state != ParseState.ReservedMaterials) // Ambiguity with Reserved Materials
                    {
                         currentLine.OrderQtyUnit = ParseString(line.Replace("- ", ""));
                    }
                    else if (line.StartsWith("WIDTH_IN:")) currentLine.WidthIn = ParseDecimal(line);
                    else if (line.StartsWith("LENGTH_IN:")) currentLine.LengthIn = ParseDecimal(line);

                    // Reserved Materials Transition
                    else if (line == "RESERVED_MATERIALS:")
                    {
                        state = ParseState.ReservedMaterials;
                        continue;
                    }

                    // Line Instructions Transition
                    else if (line == "LINE_INSTRUCTIONS:")
                    {
                        state = ParseState.LineInstructions;
                        continue;
                    }

                    // Reserved Materials Logic
                    else if (state == ParseState.ReservedMaterials)
                    {
                        if (line == "—") continue; // Empty reserved materials

                        if (line.StartsWith("- TAG_NUMBER:"))
                        {
                            currentReserved = new ReservedMaterialImportDto();
                            currentReserved.TagNumber = ParseString(line.Replace("- ", ""));
                            currentLine.ReservedMaterials.Add(currentReserved);
                        }
                        else if (currentReserved != null)
                        {
                            if (line.StartsWith("MILL_REF:")) currentReserved.MillRef = ParseString(line);
                            else if (line.StartsWith("QTY:")) currentReserved.Quantity = ParseDecimal(line);
                            else if (line.StartsWith("UNIT:")) currentReserved.Unit = ParseString(line);
                            else if (line.StartsWith("SIZE:")) currentReserved.Size = ParseString(line);
                            else if (line.StartsWith("LOC:")) currentReserved.Location = ParseString(line);
                        }
                    }

                    // Line Instructions Logic
                    else if (state == ParseState.LineInstructions)
                    {
                        // Check if we hit the next LINE
                        if (line.StartsWith("LINE:"))
                        {
                            // Backtrack? No, we handle LINE: check at top of loop.
                            // This block is only if it's NOT a new line start.
                            instructionBuffer.Add(rawLine);
                        }
                        else
                        {
                            // Could be end of file or something else
                            instructionBuffer.Add(rawLine);
                        }
                    }
                }
            }

            FlushInstructions(); // Final flush

            return dto;
        }

        private string ParseString(string line)
        {
            var parts = line.Split(':', 2);
            return parts.Length > 1 ? parts[1].Trim() : "";
        }

        private decimal ParseDecimal(string line)
        {
            var val = ParseString(line);
            // Handle commas if present? "1,234.56"
            // Use strict culture
            if (decimal.TryParse(val.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result;
            return 0m;
        }

        private int ParseInt(string line)
        {
            var val = ParseString(line);
            if (int.TryParse(val, out int result)) return result;
            return 0;
        }

        private DateTime? ParseDateTime(string line)
        {
            var val = ParseString(line);
            // Format: yyyy-mm-dd hh:mm
            if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                return result;
            return null;
        }

        private DateTime? ParseDate(string line)
        {
            var val = ParseString(line);
            // Format: yyyy-mm-dd
            if (DateTime.TryParse(val, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
                return result.Date;
            return null;
        }
    }
}
