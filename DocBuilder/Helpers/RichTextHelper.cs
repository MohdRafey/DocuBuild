using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace DocBuilder.Helpers
{
  public static class RichTextHelper
  {
    public static readonly DependencyProperty HtmlProperty =
        DependencyProperty.RegisterAttached("Html", typeof(string), typeof(RichTextHelper),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHtmlChanged));

    public static string GetHtml(DependencyObject obj) => (string)obj.GetValue(HtmlProperty);
    public static void SetHtml(DependencyObject obj, string value) => obj.SetValue(HtmlProperty, value);

    private static void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is System.Windows.Controls.RichTextBox rtb)
      {
        // 1. Check if the change is coming from the UI (user typing)
        // If the user is typing, don't force a reload of the document
        if (rtb.Tag != null && rtb.Tag.ToString() == "InternalUpdate")
        {
          rtb.Tag = null;
          return;
        }

        string html = e.NewValue as string;
        UpdateDocument(rtb, html);
      }
    }

    private static void Rtb_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (sender is System.Windows.Controls.RichTextBox rtb)
      {
        rtb.Tag = "InternalUpdate"; // Mark this as an internal UI change
        SetHtml(rtb, ConvertDocumentToHtml(rtb.Document));
      }
    }

    // --- THE ENGINE: UI TO HTML ---
    private static string ConvertDocumentToHtml(FlowDocument doc)
    {
      var sb = new StringBuilder();
      foreach (var block in doc.Blocks)
      {
        if (block is Paragraph p)
        {
          foreach (var inline in p.Inlines)
          {
            sb.Append(ParseInline(inline));
          }
        }
      }
      return sb.ToString();
    }

    private static string ParseInline(Inline inline)
    {
      if (inline is Run run)
      {
        string text = run.Text;
        if (string.IsNullOrEmpty(text)) return "";

        if (run.FontWeight == FontWeights.Bold) text = $"<b>{text}</b>";
        if (run.FontStyle == FontStyles.Italic) text = $"<i>{text}</i>";
        if (run.TextDecorations == TextDecorations.Underline) text = $"<u>{text}</u>";
        if (run.TextDecorations == TextDecorations.Strikethrough) text = $"<s>{text}</s>";

        // HIGHLIGHT CHECK: We look for Yellow background specifically
        if (run.Background is SolidColorBrush scb && scb.Color == Colors.Yellow)
        {
          text = $"<span class='marker'>{text}</span>";
        }

        return text;
      }
      if (inline is Hyperlink link)
      {
        var linkText = new StringBuilder();
        foreach (var childInline in link.Inlines) linkText.Append(ParseInline(childInline));
        return $"<a href='{link.NavigateUri}'>{linkText}</a>";
      }
      if (inline is LineBreak) return "<br/>";

      return "";
    }

    // --- THE ENGINE: HTML TO UI ---
    private static void UpdateDocument(System.Windows.Controls.RichTextBox rtb, string html)
    {
      rtb.Document.Blocks.Clear();

      // If empty, ensure there's at least one paragraph so the cursor appears
      if (string.IsNullOrEmpty(html))
      {
        rtb.Document.Blocks.Add(new Paragraph());
        return;
      }

      var p = new Paragraph();

      // Regex to split by tags: captures <b>, <i>, <u>, <s>, <a>, <span> and text
      string[] parts = System.Text.RegularExpressions.Regex.Split(html, @"(<[^>]*>|[^<]+)");

      bool isBold = false;
      bool isItalic = false;
      bool isUnderline = false;
      bool isStrike = false;
      bool isHighlight = false;
      string currentLinkUrl = null;

      foreach (var part in parts)
      {
        if (string.IsNullOrEmpty(part)) continue;

        // --- TAG DETECTION ---
        string tag = part.ToLower();

        if (tag == "<b>") isBold = true;
        else if (tag == "</b>") isBold = false;
        else if (tag == "<i>") isItalic = true;
        else if (tag == "</i>") isItalic = false;
        else if (tag == "<u>") isUnderline = true;
        else if (tag == "</u>") isUnderline = false;
        else if (tag == "<s>") isStrike = true;
        else if (tag == "</s>") isStrike = false;
        else if (tag == "<span class='marker'>") isHighlight = true;
        else if (tag == "</span>") isHighlight = false;

        // Handle Hyperlinks: <a href='url'>
        else if (tag.StartsWith("<a href="))
        {
          // Extract URL between quotes
          var match = System.Text.RegularExpressions.Regex.Match(part, @"href=['""]([^'""]+)['""]");
          if (match.Success) currentLinkUrl = match.Groups[1].Value;
        }
        else if (tag == "</a>")
        {
          currentLinkUrl = null;
        }

        // --- CONTENT PROCESSING ---
        else if (!part.StartsWith("<"))
        {
          // Decode HTML entities (e.g., &amp; -> &) for the UI
          string decodedText = System.Net.WebUtility.HtmlDecode(part);

          Run run = new Run(decodedText);

          // Apply styles based on active flags
          if (isBold) run.FontWeight = FontWeights.Bold;
          if (isItalic) run.FontStyle = FontStyles.Italic;

          // Handle multiple decorations (Underline + Strike)
          TextDecorationCollection decorations = new TextDecorationCollection();
          if (isUnderline) foreach (var d in TextDecorations.Underline) decorations.Add(d);
          if (isStrike) foreach (var d in TextDecorations.Strikethrough) decorations.Add(d);
          run.TextDecorations = decorations;

          if (isHighlight) run.Background = System.Windows.Media.Brushes.Yellow;

          // If we are inside an <a> tag, wrap the Run in a Hyperlink object
          if (!string.IsNullOrEmpty(currentLinkUrl))
          {
            try
            {
              var link = new Hyperlink(run);
              link.NavigateUri = new Uri(currentLinkUrl);
              p.Inlines.Add(link);
            }
            catch { p.Inlines.Add(run); } // Fallback to normal text if URL is invalid
          }
          else
          {
            p.Inlines.Add(run);
          }
        }
      }

      rtb.Document.Blocks.Add(p);
    }


  }
}