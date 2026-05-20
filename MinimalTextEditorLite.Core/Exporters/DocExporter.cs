using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlToOpenXml;
using MinimalTextEditorLite.Exporters.Contracts.EditorJs;

namespace MinimalTextEditorLite.Core.Exporters;

public sealed class DocExporter : IExporter
{
    private readonly HtmlDocumentBuilder htmlBuilder;

    public DocExporter(HtmlDocumentBuilder htmlBuilder)
    {
        this.htmlBuilder = htmlBuilder;
    }

    public string Id => "doc";

    public string DisplayName => "DOCX";

    public string DefaultFileName => "Note.docx";

    public string FileDialogFilter => "Word Documents (*.docx)|*.docx";

    public Task<byte[]> ExportAsync(ExportContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // HtmlConverter é síncrono. Mantemos em background para não bloquear a UI
        // caso o usuário exporte documentos maiores.
        return Task.Run(() => GenerateDocxBytes(context.Document));
    }

    private byte[] GenerateDocxBytes(EditorJsDocument document)
    {
        var html = htmlBuilder.Build(document, new HtmlBuildOptions
        {
            Variant = HtmlVariant.Standard,
            DocumentTitle = "Note Export"
        });

        using var stream = new MemoryStream();

        using (var wordDocument = WordprocessingDocument.Create(
            stream,
            DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            var converter = new HtmlConverter(mainPart)
            {
                // Mantém hyperlinks clicáveis.
                SupportsAnchorLinks = true,

                // Embute imagens suportadas no .docx.
                ImageProcessing = ImageProcessingMode.Embed
            };

            var body = mainPart.Document.Body;
            if (body is null)
                throw new InvalidOperationException("DOCX main document body was not initialized.");

            foreach (var element in converter.Parse(html))
                body.Append(element);

            PolishDocument(mainPart);

            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static void PolishDocument(MainDocumentPart mainPart)
    {
        var document = mainPart.Document
            ?? throw new InvalidOperationException("DOCX main document was not initialized.");
        var body = document.Body;

        if (body is null)
            throw new InvalidOperationException("DOCX main document body was not initialized.");

        ApplyPageLayout(body);
        ApplyParagraphPolish(body);
        ApplyTablePolish(body);
    }

    private static void ApplyPageLayout(Body body)
    {
        var sectionProperties = body.Elements<SectionProperties>().LastOrDefault();

        if (sectionProperties is null)
        {
            sectionProperties = new SectionProperties();
            body.Append(sectionProperties);
        }

        var pageSize = sectionProperties.GetFirstChild<PageSize>();
        if (pageSize is null)
        {
            pageSize = new PageSize();
            sectionProperties.PrependChild(pageSize);
        }

        // A4 portrait em twentieths of a point.
        pageSize.Width = UInt32Value.FromUInt32(11906);
        pageSize.Height = UInt32Value.FromUInt32(16838);

        var pageMargin = sectionProperties.GetFirstChild<PageMargin>();
        if (pageMargin is null)
        {
            pageMargin = new PageMargin();
            sectionProperties.Append(pageMargin);
        }

        // 0.75" — aproxima o DOCX do PDF da V2.1 e deixa mais respiro.
        pageMargin.Top = Int32Value.FromInt32(1080);
        pageMargin.Right = UInt32Value.FromUInt32(1080);
        pageMargin.Bottom = Int32Value.FromInt32(1080);
        pageMargin.Left = UInt32Value.FromUInt32(1080);
        pageMargin.Header = UInt32Value.FromUInt32(720);
        pageMargin.Footer = UInt32Value.FromUInt32(720);
        pageMargin.Gutter = UInt32Value.FromUInt32(0);
    }

    private static void ApplyParagraphPolish(Body body)
    {
        foreach (var paragraph in body.Descendants<Paragraph>())
        {
            var paragraphProperties = EnsureParagraphProperties(paragraph);
            var styleId = paragraphProperties.ParagraphStyleId?.Val?.Value;
            var headingLevel = GetHeadingLevel(styleId);
            var insideTable = paragraph.Ancestors<TableCell>().Any();

            paragraphProperties.GetFirstChild<SpacingBetweenLines>()?.Remove();

            if (headingLevel > 0)
            {
                paragraphProperties.Append(new SpacingBetweenLines
                {
                    Before = headingLevel == 1 ? "260" : "220",
                    After = "120",
                    Line = "276",
                    LineRule = LineSpacingRuleValues.Auto
                });
            }
            else if (insideTable)
            {
                paragraphProperties.Append(new SpacingBetweenLines
                {
                    Before = "0",
                    After = "80",
                    Line = "260",
                    LineRule = LineSpacingRuleValues.Auto
                });
            }
            else
            {
                paragraphProperties.Append(new SpacingBetweenLines
                {
                    Before = "80",
                    After = "140",
                    Line = "300",
                    LineRule = LineSpacingRuleValues.Auto
                });
            }

            var fontSize = GetFontSizeForHeadingLevel(headingLevel);

            foreach (var run in paragraph.Elements<Run>())
                ApplyRunDefaults(run, fontSize, headingLevel > 0);
        }
    }

    private static ParagraphProperties EnsureParagraphProperties(Paragraph paragraph)
    {
        var paragraphProperties = paragraph.GetFirstChild<ParagraphProperties>();

        if (paragraphProperties is not null)
            return paragraphProperties;

        paragraphProperties = new ParagraphProperties();
        paragraph.PrependChild(paragraphProperties);

        return paragraphProperties;
    }

    private static void ApplyRunDefaults(Run run, string fontSize, bool forceBold)
    {
        var runProperties = run.GetFirstChild<RunProperties>();

        if (runProperties is null)
        {
            runProperties = new RunProperties();
            run.PrependChild(runProperties);
        }

        if (runProperties.GetFirstChild<RunFonts>() is null)
        {
            runProperties.Append(new RunFonts
            {
                Ascii = "Segoe UI",
                HighAnsi = "Segoe UI",
                EastAsia = "Segoe UI",
                ComplexScript = "Segoe UI"
            });
        }

        if (runProperties.GetFirstChild<FontSize>() is null)
            runProperties.Append(new FontSize { Val = fontSize });

        if (runProperties.GetFirstChild<FontSizeComplexScript>() is null)
            runProperties.Append(new FontSizeComplexScript { Val = fontSize });

        if (forceBold && runProperties.GetFirstChild<Bold>() is null)
            runProperties.Append(new Bold());

        if (runProperties.GetFirstChild<Color>() is null)
            runProperties.Append(new Color { Val = "1A1A1A" });
    }

    private static int GetHeadingLevel(string? styleId)
    {
        if (string.IsNullOrWhiteSpace(styleId))
            return 0;

        for (var level = 1; level <= 6; level++)
        {
            if (string.Equals(styleId, $"Heading{level}", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(styleId, $"Heading {level}", StringComparison.OrdinalIgnoreCase))
            {
                return level;
            }
        }

        return 0;
    }

    private static string GetFontSizeForHeadingLevel(int headingLevel)
    {
        return headingLevel switch
        {
            1 => "36", // 18pt
            2 => "30", // 15pt
            3 => "26", // 13pt
            4 => "24", // 12pt
            5 => "22", // 11pt
            6 => "21", // 10.5pt
            _ => "22"  // 11pt
        };
    }

    private static void ApplyTablePolish(Body body)
    {
        foreach (var table in body.Descendants<Table>())
        {
            ApplyTableProperties(table);
            ApplyFirstRowAsSoftHeader(table);
        }
    }

    private static void ApplyTableProperties(Table table)
    {
        var tableProperties = table.GetFirstChild<TableProperties>();

        if (tableProperties is null)
        {
            tableProperties = new TableProperties();
            table.PrependChild(tableProperties);
        }

        tableProperties.GetFirstChild<TableWidth>()?.Remove();
        tableProperties.Append(new TableWidth
        {
            Width = "5000",
            Type = TableWidthUnitValues.Pct
        });

        tableProperties.GetFirstChild<TableBorders>()?.Remove();
        tableProperties.Append(new TableBorders(
            new TopBorder { Val = BorderValues.Single, Size = 4U, Color = "D9D9D9" },
            new LeftBorder { Val = BorderValues.Single, Size = 4U, Color = "D9D9D9" },
            new BottomBorder { Val = BorderValues.Single, Size = 4U, Color = "D9D9D9" },
            new RightBorder { Val = BorderValues.Single, Size = 4U, Color = "D9D9D9" },
            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4U, Color = "D9D9D9" },
            new InsideVerticalBorder { Val = BorderValues.Single, Size = 4U, Color = "D9D9D9" }));
    }

    private static void ApplyFirstRowAsSoftHeader(Table table)
    {
        var firstRow = table.Elements<TableRow>().FirstOrDefault();
        if (firstRow is null)
            return;

        foreach (var cell in firstRow.Elements<TableCell>())
        {
            var cellProperties = cell.GetFirstChild<TableCellProperties>();

            if (cellProperties is null)
            {
                cellProperties = new TableCellProperties();
                cell.PrependChild(cellProperties);
            }

            cellProperties.GetFirstChild<Shading>()?.Remove();
            cellProperties.Append(new Shading
            {
                Val = ShadingPatternValues.Clear,
                Color = "auto",
                Fill = "F0F0F0"
            });

            foreach (var run in cell.Descendants<Run>())
            {
                var runProperties = run.GetFirstChild<RunProperties>();

                if (runProperties is null)
                {
                    runProperties = new RunProperties();
                    run.PrependChild(runProperties);
                }

                if (runProperties.GetFirstChild<Bold>() is null)
                    runProperties.Append(new Bold());
            }
        }
    }
}
