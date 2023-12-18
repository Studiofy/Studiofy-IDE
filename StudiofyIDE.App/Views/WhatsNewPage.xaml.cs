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

        private List<UIElement> ConvertMarkdownToUIElements(List<Block> markdownBlocks)
        {
            List<UIElement> uiElements = new();

            double previousBottomMargin = 0;

            foreach (Block block in markdownBlocks)
            {
                double currentBottomMargin = previousBottomMargin + 10;

                if (block is HeadingBlock heading)
                {
                    // Create a TextBlock for headings
                    TextBlock headingTextBlock = new()
                    {
                        Text = heading.Inline.FirstChild.ToString(),
                        FontSize = 18,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Margin = new Thickness(0, currentBottomMargin * 2, 0, 0)
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
                            Margin = new Thickness(15, currentBottomMargin * 4, 0, 0) // Adjusted margin for list items
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
                                Margin = new Thickness(0, currentBottomMargin * 0, 5, 0)
                            };

                            uiElements.Add(hyperlink);
                        }
                        else
                        {
                            // For other inline elements, let's add a TextBlock
                            TextBlock textBlock = new()
                            {
                                Text = inline.NextSibling.ToString(),
                                Margin = new Thickness(0, currentBottomMargin, 5, 0)
                            };

                            uiElements.Add(textBlock);
                        }
                    }
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

            foreach (UIElement uiElement in uiElements)
            {
                ReleaseBody.Children.Add(uiElement);
            }
        }
    }
}
