// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace WindowsCode.Studio.Views.TabViews
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EditBoxTabView : Page
    {
        public EditBoxTabView()
        {
            InitializeComponent();
        }

        private void CodeEditor_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            //CoreVirtualKeyStates shiftState = CoreWindow.GetForCurrentThread().GetKeyState(VirtualKey.Shift);
            // Check if the tab key is pressed
            if (e.Key == VirtualKey.Tab)
            {
                // Check if the shift key is pressed
                //if (shiftState.HasFlag(CoreVirtualKeyStates.Down))
                //{
                // Delete four spaces
                //    CodeEditor.Document.Selection.Delete(Microsoft.UI.Text.TextRangeUnit.Character, 4);
                //}
                //else
                //{
                // Insert four spaces
                CodeEditor.Document.Selection.TypeText("    ");
                //}
                // Mark the event as handled
                e.Handled = true;
            }
            // Check if the backspace or delete key is pressed
            //else if (e.Key == VirtualKey.Back || e.Key == VirtualKey.Delete)
            //{
            //    // Delete the selected text
            //    CodeEditor.Document.Selection.Delete(Microsoft.UI.Text.TextRangeUnit.CharacterFormat, 0);
            //    // Mark the event as handled
            //    e.Handled = true;
            //}
        }
    }
}
