using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using WindowsCode.Studio.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WindowsCode.Studio.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WhatsNewPage : Page
    {
        public WhatsNewPage()
        {
            InitializeComponent();
        }

        private bool TryParseMarkdownLink(string text, out string linkText, out string linkUrl)
        {
            linkText = null;
            linkUrl = null;

            if (text.Contains("[") && text.Contains("]("))
            {
                int startBracketIndex = text.IndexOf('[');
                int endBracketIndex = text.IndexOf(']');
                int startParenthesisIndex = text.IndexOf('(');
                int endParenthesisIndex = text.IndexOf(')');

                if (startBracketIndex < endBracketIndex && endBracketIndex < startParenthesisIndex &&
                    startParenthesisIndex < endParenthesisIndex)
                {
                    // Adjusted indices to include the characters at the specified positions
                    linkText = text.Substring(startBracketIndex + 1, endBracketIndex - startBracketIndex - 1);
                    linkUrl = text.Substring(startParenthesisIndex + 1, endParenthesisIndex - startParenthesisIndex - 1);

                    return true;
                }
            }

            return false;
        }


        private List<UIElement> ConvertMarkdownToUIElements(List<Block> markdownBlocks)
        {
            List<UIElement> uiElements = new();

            double previousBottomMargin = 0;

            foreach (Block block in markdownBlocks)
            {
                double currentBottomMargin = previousBottomMargin;

                if (block is HeadingBlock heading)
                {
                    // Create a TextBlock for headings
                    TextBlock headingTextBlock = new()
                    {
                        Text = heading.Inline.FirstChild.ToString(),
                        FontSize = 24,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Margin = new Thickness(0, currentBottomMargin + 10, 0, currentBottomMargin + 10)
                    };

                    uiElements.Add(headingTextBlock);
                }
                else if (block is ListBlock listBlock)
                {
                    foreach (ListItemBlock listItem in listBlock.OfType<ListItemBlock>())
                    {
                        string listItemText = "";

                        // Check if the ListItemBlock has a ParagraphBlock child
                        if (listItem.FirstOrDefault() is ParagraphBlock paragraph)
                        {
                            listItemText = string.Join("", paragraph.Inline.Cast<Inline>().Select(inline => inline is LiteralInline ? ((LiteralInline)inline).Content.ToString() : ""));
                        }

                        // Create a TextBlock for list items
                        TextBlock listItemTextBlock = new()
                        {
                            Text = $"{listItemText}",
                            Margin = new Thickness(15, currentBottomMargin, 0, 0) // Adjusted margin for list items
                        };

                        uiElements.Add(listItemTextBlock);
                    }
                }
                else if (block is ParagraphBlock paragraph)
                {
                    foreach (Inline inline in paragraph.Inline)
                    {
                        if (inline is LinkInline linkInline)
                        {
                            // Create a hyperlink control for links
                            HyperlinkButton hyperlink = new()
                            {
                                Content = linkInline.FirstChild.ToString(),
                                NavigateUri = new Uri(linkInline.Url),
                                Margin = new Thickness(0, currentBottomMargin, 5, 0)
                            };

                            uiElements.Add(hyperlink);
                        }
                        else if (inline is LiteralInline literalInline)
                        {
                            string textContent = literalInline.Content.ToString();

                            // Check for Markdown hyperlink format [text](url)
                            if (TryParseMarkdownLink(textContent, out string linkText, out string linkUrl))
                            {
                                HyperlinkButton hyperlink = new()
                                {
                                    Content = linkText,
                                    NavigateUri = new Uri(linkUrl),
                                    Margin = new Thickness(15, currentBottomMargin, 5, 0) // Adjusted margin for content under headings
                                };

                                uiElements.Add(hyperlink);
                            }
                            else if (textContent.Contains("#") && int.TryParse(textContent.Substring(1), out int pullRequestId))
                            {
                                // Assuming the base URL for pull requests
                                string baseUrl = "https://github.com/Studiofy/Studiofy-IDE/pull/";

                                HyperlinkButton prLinkButton = new()
                                {
                                    Content = textContent,
                                    NavigateUri = new Uri($"{baseUrl}{pullRequestId}"),
                                    Margin = new Thickness(15, currentBottomMargin, 5, 0) // Adjusted margin for content under headings
                                };

                                uiElements.Add(prLinkButton);
                            }
                            else
                            {
                                // For other inline elements, let's add a TextBlock
                                TextBlock textBlock = new()
                                {
                                    Text = textContent,
                                    Margin = new Thickness(15, currentBottomMargin, 5, 0) // Adjusted margin for content under headings
                                };

                                uiElements.Add(textBlock);
                            }
                        }
                        else if (inline is EmphasisInline emphasisInline)
                        {
                            // For emphasized text, let's add a TextBlock with italic font style
                            TextBlock textBlock = new()
                            {
                                Text = emphasisInline.ToString(),
                                Margin = new Thickness(0, currentBottomMargin, 5, 0)
                            };

                            uiElements.Add(textBlock);
                        }
                    }
                }
                else if (block is QuoteBlock quote)
                {
                    // Create a TextBlock for quotes
                    TextBlock quoteTextBlock = new()
                    {
                        Text = quote.QuoteLines.ToString(),
                        Margin = new Thickness(0, currentBottomMargin * 2, 0, 0)
                    };

                    uiElements.Add(quoteTextBlock);
                }

                previousBottomMargin = currentBottomMargin;
            }

            return uiElements;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            string content = await new UpdateService().GetUpdateDescription(Content.XamlRoot);

            List<Block> markdownBlocks = new UpdateService().ParseMarkdown(content);

            List<UIElement> uiElements = ConvertMarkdownToUIElements(markdownBlocks);

            // Clear existing UIElements in ReleaseBody
            ReleaseBody.Children.Clear();

            foreach (UIElement uiElement in uiElements)
            {
                ReleaseBody.Children.Add(uiElement);
            }
        }
    }
}
