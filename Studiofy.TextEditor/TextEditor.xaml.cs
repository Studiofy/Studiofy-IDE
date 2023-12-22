using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Studiofy.IDE.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Line = Studiofy.IDE.Models.Line;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Studiofy.TextEditor
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TextEditor : Page
    {
        public static EditorPage Current;

        private int charOffset = 0;

        private int visibleChars { get => int.Parse(EditControl.ActualWidth - Width_Left) / CharWidth; }

        private int _visibleLines { get => int.Parse(EditControl.ActualHeight.ToString()) / CharHeight; }

        private bool isSettingValue = false;

        private int maxChars = 0;

        public bool isInitialized = false;

        public event ErrorEventHandler errorHandler;

        public event EventHandler<string> errorMessage;

        public event EventHandler doubleClicked;

        public event PropertyChangedEventHandler textChanged;

        public event PropertyChangedEventHandler linesChanged;

        public event PropertyChangedEventHandler cursorPlaceChanged;

        public event EventHandler initialized;

        public Pointer CursorPoint { get => GetValue(new Point()); set => SetValue(value); }

        public bool isSelection { get => GetValue(false); set => SetValue(value); }

        public Place CursorPlace
        {
            get => GetValue(new Place(0, 0));
            set
            {
                SetValue(value);

                if (isCanvasLoaded && !isSelecting)
                {
                    if (!isLineSelect)
                    {
                        double width = Parent.ActualWidth - Width_Left;
                        if (value.iChar * CharWidth < HorizontalScrollBar.Value)
                            HorizontalScrollBar.Value = value.iChar * CharWidth;
                        else if ((value.iChar + 3) * CharWidth - width - HorizontalScrollBar.Value > 0)
                            HorizontalScrollBar.Value = Math.Max((value.iChar + 3) * CharWidth - width, 0);
                    }

                    if ((value.iLine + 1) * CharHeight <= VerticalScrollBar.Value)
                        VerticalScrollBar.Value = value.iLine * CharHeight;
                    else if ((value.iLine + 2) * CharHeight > VerticalScrollBar.Value + Parent.ActualHeight)
                        VerticalScrollBar.Value = Math.Min((value.iLine + 2) * CharHeight - Parent.ActualHeight, VerticalScrollBar.Maximum);
                }

                int x = CharWidth * (value.iChar - charOffset) + Width_Left;
                int startline = visibleLines.Count > 0 ? visibleLines[0].LineNumber : 0;
                int y = CharHeight * (value.iLine - startline + 1);
                CursorPoint = new Point(x, y + CharHeight);

                isSettingValue = true;
                currentLine = value;
                isSettingValue = false;

                CanvasBeam.Invalidate();

            }
        }

        public bool isSuggesting
        {
            get => GetValue(false); set
            {
                SetValue(value);
                if (value)
                {
                    suggestionIndex = -1;
                }
            }
        }

        public ObservableCollection<Line> lines { get => GetValue(new ObservableCollection<Line>()); set => SetValue(value); }

        public string selectedText
        {
            get
            {
                string text = "";
                if (Selection.Start == Selection.End)
                    return "";

                Place start = Selection.VisualStart;
                Place end = Selection.VisualEnd;

                if (start.iLine == end.iLine)
                {
                    text = lines[start.iLine].LineText.Substring(start.iChar, end.iChar - start.iChar);
                }
                else
                {
                    for (int iLine = start.iLine; iLine <= end.iLine; iLine++)
                    {
                        if (iLine == start.iLine)
                            text += lines[iLine].LineText.Substring(start.iChar) + "\r\n";
                        else if (iLine == end.iLine)
                            text += lines[iLine].LineText.Substring(0, end.iChar);
                        else
                            text += lines[iLine].LineText + "\r\n";
                    }
                }

                return text;
            }
        }

        public List<Line> selectedLines = new();

        public Models.Range Selection
        {
            get => GetValue(new Models.Range(CursorPlace, CursorPlace));
            set
            {
                SetValue(value);
                CursorPlace = new Place(value.End.iChar, value.End.iLine);
                isSelection = value.Start != value.End;
                selectedLines = lines.ToList().Where(x => x != null && x.iLine >= value.VisualStart.iLine && x.iLine <= value.VisualEnd.iLine).ToList();
                CanvasSelection.Invalidate();
            }
        }

        public List<SyntaxError> syntaxErrors
        {
            get => GetValue(new List<SyntaxError>()); set
            {
                SetValue(value);
                DispatcherQueue.TryEnqueue(() =>
                {
                    CanvasText.Invalidate();
                });
            }
        }

        public List<Line> visibleLines { get; set; } = new();

        private bool isCanvasLoaded { get => CanvasText.IsLoaded; }

        private bool isLineSelect { get; set; } = false;

        private bool isMiddleClickScrolling { get => GetValue(false); set => SetValue(value); }

        private bool IsFocused { get => GetValue(false); set => SetValue(value); }

        private bool isSelecting { get; set; } = false;

        public Models.Char this[Place place]
        {
            get => lines[place.iLine][place.iChar];
            set => lines[place.iLine][place.iChar] = value;
        }

        public Line this[int iLine]
        {
            get { return lines[iLine]; }
        }

        public static int IntLength(int i)
        {
            if (i < 0)
                return 1;
            if (i == 0)
                return 1;
            return (int)Math.Floor(Math.Log10(i)) + 1;
        }

        public EditorPage()
        {
            InitializeComponent();

            Current = this;
        }

        public void RedrawText()
        {
            try
            {
                CanvasText.Invalidate();
            }
            catch (Exception ex)
            {
                errorHandler?.Invoke(this, new ErrorEventArgs(ex));
            }
        }

        public void Invalidate(bool sizeChanged = false)
        {
            try
            {
                DrawText(sizeChanged);
            }
            catch (Exception ex)
            {
                errorHandler?.Invoke(this, new ErrorEventArgs(ex));
            }
        }

        public async void SaveAsync()
        {
            await Task.Run(() =>
            {
                foreach (Line line in new List<Line>(lines))
                {
                    line.Save();
                }
                syntaxErrors.Clear();
            });

            isSuggesting = false;
            isSuggestingOptions = false;

            CanvasText.Invalidate();
        }



        private void Parent_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {

        }

        private void Parent_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        private void ParentContent_KeyDown(object sender, KeyRoutedEventArgs e)
        {

        }

        private void ParentContent_KeyUp(object sender, KeyRoutedEventArgs e)
        {

        }

        private void ParentContent_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {

        }

        private void ParentContent_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {

        }

        private void EditControl_DragStarting(UIElement sender, DragStartingEventArgs args)
        {

        }

        private void EditControl_DragEnter(object sender, DragEventArgs e)
        {

        }

        private void EditControl_DragOver(object sender, DragEventArgs e)
        {

        }

        private void EditControl_Drop(object sender, DragEventArgs e)
        {

        }

        private void EditControl_PointerMoved(object sender, PointerRoutedEventArgs e)
        {

        }

        private void EditControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {

        }

        private void EditControl_PointerReleased(object sender, PointerRoutedEventArgs e)
        {

        }

        private void EditControl_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {

        }

        private void EditControl_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {

        }

        private void EditControl_PointerExited(object sender, PointerRoutedEventArgs e)
        {

        }

        private void CanvasSelection_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            try
            {
                if (visibleLines.Count > 0)
                {
                    if (IsSelection)
                    {
                        Place start = Selection.VisualStart;
                        Place end = Selection.VisualEnd;

                        if (start.iLine < visibleLines[0].iLine)
                        {
                            start.iLine = visibleLines[0].iLine;
                            start.iChar = 0;
                        }

                        if (end.iLine > visibleLines.Last().iLine)
                        {
                            end.iLine = visibleLines.Last().iLine;
                            end.iChar = visibleLines.Last().Count;
                        }

                        for (int lp = start.iLine; lp <= end.iLine; lp++)
                            if (lp >= visibleLines[0].iLine && lp <= visibleLines.Last().iLine)
                                if (start.iLine == end.iLine)
                                    DrawSelection(args.DrawingSession, start.iLine, start.iChar, end.iChar);
                                else if (lp == start.iLine)
                                    DrawSelection(args.DrawingSession, lp, start.iChar, Lines[lp].Count + 1);
                                else if (lp > start.iLine && lp < end.iLine)
                                    DrawSelection(args.DrawingSession, lp, 0, Lines[lp].Count + 1);
                                else if (lp == end.iLine)
                                    DrawSelection(args.DrawingSession, lp, 0, end.iChar);
                    }

                    foreach (SearchMatch match in new List<SearchMatch>(SearchMatches))
                    {
                        if (match.iLine >= visibleLines[0].iLine && match.iLine <= visibleLines.Last().iLine)
                            DrawSelection(args.DrawingSession, match.iLine, match.iChar, match.iChar + match.Match.Length, SelectionType.SearchMatch);
                    }
                }
            }
            catch (Exception ex)
            {
                errorHandler?.Invoke(this, new ErrorEventArgs(ex));
            }
        }

        private void CanvasBeam_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            try
            {
                if (visibleLines.Count > 0)
                {
                    int x = (int)(Width_Left + HorizontalOffset + CursorPlace.iChar * CharWidth);
                    int y = (int)((CursorPlace.iLine - visibleLines[0].LineNumber + 1) * CharHeight - 1 / 2 * CharHeight);

                    for (int i = 0; i < CursorPlace.iChar; i++)
                    {
                        if (lines.Count > CursorPlace.iLine)
                            if (lines[CursorPlace.iLine].Count > i)
                                if (lines[CursorPlace.iLine][i].C == '\t')
                                {
                                    x += CharWidth * (TabLength - 1);
                                }
                    }

                    Point point = PlaceToPoint(CursorPlace);
                    y = (int)point.Y;
                    x = (int)point.X;

                    if (Selection.Start == CursorPlace)
                    {
                        //args.DrawingSession.DrawRoundedRectangle(Width_Left, y, (int)EditControl.ActualWidth - Width_Left, CharHeight, 2, 2, ActualTheme == ElementTheme.Light ? Color_FoldingMarker.InvertColorBrightness() : Color_FoldingMarker, 2f);
                        args.DrawingSession.FillRectangle(Width_Left, y, (int)EditControl.ActualWidth - Width_Left, CharHeight, ActualTheme == ElementTheme.Light ? Color_SelelectedLineBackground.InvertColorBrightness() : Color_SelelectedLineBackground);
                    }

                    if (y <= EditControl.ActualHeight && y >= 0 && x <= EditControl.ActualWidth && x >= Width_Left)
                        args.DrawingSession.DrawLine(new Vector2(x, y), new Vector2(x, y + CharHeight), ActualTheme == ElementTheme.Light ? Color_Beam.InvertColorBrightness() : Color_Beam, 2f);


                    int xms = (int)(Width_Left);
                    int iCharStart = charOffset;
                    int xme = (int)EditControl.ActualWidth;
                    int iCharEnd = iCharStart + (int)((xme - xms) / CharWidth);

                    if (ShowHorizontalTicks)
                        for (int iChar = iCharStart; iChar < iCharEnd; iChar++)
                        {
                            int xs = (int)((iChar - iCharStart) * CharWidth) + xms;
                            if (iChar % 10 == 0)
                                args.DrawingSession.DrawLine(xs, 0, xs, CharHeight / 8, new CanvasSolidColorBrush(sender, Color_LineNumber), 2f);
                        }
                }
            }
            catch (Exception ex)
            {
                errorHandler?.Invoke(this, new ErrorEventArgs(ex));
            }
        }

        private void CanvasText_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            try
            {
                sender.DpiScale = XamlRoot.RasterizationScale > 1.0d ? 1.15f : 1.0f; // The text was shaking around on text input at Scale factors > 1. Setting DpiScale seems to prevent this.
                args.DrawingSession.Antialiasing = CanvasAntialiasing.Aliased;
                //args.DrawingSession.Blend = CanvasBlend.Add;
                //args.DrawingSession.TextAntialiasing = CanvasTextAntialiasing.ClearType;
                if (VisibleLines.Count > 0)
                {
                    int foldPos = Width_LeftMargin + Width_LineNumber + Width_ErrorMarker + Width_WarningMarker;
                    int errorPos = Width_LeftMargin + Width_LineNumber;
                    int warningPos = errorPos + Width_ErrorMarker;
                    int totalwraps = 0;
                    float thickness = Math.Max(1, CharWidth / 6f);

                    for (int iLine = VisibleLines[0].iLine; iLine < VisibleLines.Last().LineNumber; iLine++)
                    {
                        int y = CharHeight * (iLine - VisibleLines[0].LineNumber + 1 + totalwraps);
                        int x = 0;
                        args.DrawingSession.FillRectangle(0, y, Width_Left - Width_TextIndent, CharHeight, Color_LeftBackground);

                        if (ShowLineNumbers)
                            args.DrawingSession.DrawText((iLine + 1).ToString(), CharWidth * IntLength(Lines.Count) + Width_LeftMargin, y, ActualTheme == ElementTheme.Light ? Color_LineNumber.InvertColorBrightness() : Color_LineNumber, new CanvasTextFormat() { FontFamily = FontUri, FontSize = ScaledFontSize, HorizontalAlignment = CanvasHorizontalAlignment.Right });
                        if (IsFoldingEnabled && Language.FoldingPairs != null)
                        {

                            if (foldings.Any(x => x.StartLine == iLine))
                            {
                                //args.DrawingSession.FillCircle(foldPos + CharWidth / 2, y + CharHeight / 2, CharWidth / 3, ActualTheme == ElementTheme.Light ? Color_FoldingMarker.InvertColorBrightness() : Color_FoldingMarker);
                                float w = CharWidth * 0.75f;
                                args.DrawingSession.FillRectangle(foldPos + (CharWidth - w) / 2f, y + CharHeight / 2 - w / 2f, w, w, ActualTheme == ElementTheme.Light ? Color_FoldingMarker.InvertColorBrightness() : Color_FoldingMarker);
                                //args.DrawingSession.DrawLine(foldPos + CharWidth / 4, y + CharHeight / 2, foldPos + CharWidth * 3 / 4, y + CharHeight / 2, ActualTheme == ElementTheme.Light ? Color_FoldingMarker.InvertColorBrightness() : Color_FoldingMarker, 2);
                            }

                            //else if ()
                            //{
                            //	args.DrawingSession.DrawLine(foldPos + CharWidth / 2, y, foldPos + CharWidth / 2, y + CharHeight, ActualTheme == ElementTheme.Light ? Color_FoldingMarker.InvertColorBrightness() : Color_FoldingMarker, 2);
                            //	args.DrawingSession.DrawLine(foldPos + CharWidth / 2, y + CharHeight / 2, foldPos + CharWidth, y + CharHeight / 2, ActualTheme == ElementTheme.Light ? Color_FoldingMarker.InvertColorBrightness() : Color_FoldingMarker, 2);
                            //}
                            else if (foldings.Any(x => x.Endline == iLine))
                            {
                                args.DrawingSession.DrawLine(foldPos + CharWidth / 2f - thickness / 2f, y + CharHeight / 2f, foldPos + CharWidth, y + CharHeight / 2f, ActualTheme == ElementTheme.Light ? Color_FoldingMarker.InvertColorBrightness() : Color_FoldingMarker, thickness);
                                args.DrawingSession.DrawLine(foldPos + CharWidth / 2f, y, foldPos + CharWidth / 2f, y + CharHeight / 2f, ActualTheme == ElementTheme.Light ? Color_FoldingMarker.InvertColorBrightness() : Color_FoldingMarker, thickness);
                            }

                            if (foldings.Any(x => iLine > x.StartLine && iLine < x.Endline))
                            {
                                args.DrawingSession.DrawLine(foldPos + CharWidth / 2f, y - CharHeight / 2f, foldPos + CharWidth / 2f, y + CharHeight * 1.5f, ActualTheme == ElementTheme.Light ? Color_FoldingMarker.InvertColorBrightness() : Color_FoldingMarker, thickness);
                            }
                        }

                        if (ShowLineMarkers)
                        {
                            if (Lines[iLine].IsUnsaved)
                                args.DrawingSession.FillRectangle(warningPos, y, Width_ErrorMarker, CharHeight, ActualTheme == ElementTheme.Light ? Color_UnsavedMarker.ChangeColorBrightness(-0.2f) : Color_UnsavedMarker);

                            if (SyntaxErrors.Any(x => x.iLine == iLine))
                            {
                                SyntaxError lineError = SyntaxErrors.First(x => x.iLine == iLine);
                                if (lineError.SyntaxErrorType == SyntaxErrorType.Error)
                                {
                                    args.DrawingSession.FillRectangle(errorPos, y, Width_ErrorMarker, CharHeight, Color.FromArgb(255, 200, 40, 40));
                                }
                                if (lineError.SyntaxErrorType == SyntaxErrorType.Warning)
                                {
                                    args.DrawingSession.FillRectangle(warningPos, y, Width_WarningMarker, CharHeight, Color.FromArgb(255, 180, 180, 40));
                                }
                            }
                        }

                        int lastChar = IsWrappingEnabled ? Lines[iLine].Count : Math.Min(charOffset + ((int)Scroll.ActualWidth - Width_Left) / CharWidth, Lines[iLine].Count);
                        int indents = 0;

                        int textWrappingLines = Lines[iLine].Count / ((int)Scroll.ActualWidth - Width_Left);
                        int linewraps = 0;
                        int wrapindent = 0;
                        int iWrappingChar = 0;

                        if (IsWrappingEnabled)
                        {
                            for (int iWrappedLine = 0; iWrappedLine < Lines[iLine].WrappedLines.Count; iWrappedLine++)
                            {
                                List<Char> wrappedLine = Lines[iLine].WrappedLines[iWrappedLine];
                                for (int iChar = 0; iChar < wrappedLine.Count; iChar++)
                                {
                                    Char c = wrappedLine[iChar];

                                    if (c.C == '\t')
                                    {
                                        x = Width_Left + CharWidth * (iChar + indents * (TabLength - 1) - charOffset);
                                        indents += 1;
                                    }
                                    else if (iChar >= charOffset - indents * (TabLength - 1))
                                    {

                                        //	int iWrappingEndPosition = (linewraps + 1) * maxchar - (WrappingLength * linewraps) - (indents * (TabLength - 1) * (linewraps + 1));

                                        if (iChar > 0 && iChar < wrappedLine.Count)
                                            iWrappingChar++;


                                        x = Width_Left + CharWidth * (iWrappingChar - charOffset + indents * (TabLength - 1));
                                        //}

                                        if (c.T == Token.Key)
                                        {
                                            if (IsInsideBrackets(new(iChar, iLine)))
                                            {
                                                args.DrawingSession.DrawText(c.C.ToString(), x, y, ActualTheme == ElementTheme.Light ? EditorOptions.TokenColors[Token.Key].InvertColorBrightness() : EditorOptions.TokenColors[Token.Key], new CanvasTextFormat() { FontFamily = FontUri, FontSize = ScaledFontSize });
                                            }
                                            else
                                            {
                                                args.DrawingSession.DrawText(c.C.ToString(), x, y, ActualTheme == ElementTheme.Light ? EditorOptions.TokenColors[Token.Normal].InvertColorBrightness() : EditorOptions.TokenColors[Token.Normal], new CanvasTextFormat() { FontFamily = FontUri, FontSize = ScaledFontSize });
                                            }
                                        }
                                        else
                                        {
                                            args.DrawingSession.DrawText(c.C.ToString(), x, y, ActualTheme == ElementTheme.Light ? EditorOptions.TokenColors[c.T].InvertColorBrightness() : EditorOptions.TokenColors[c.T], new CanvasTextFormat() { FontFamily = FontUri, FontSize = ScaledFontSize });
                                        }

                                    }

                                }
                                if (iWrappedLine < Lines[iLine].WrappedLines.Count - 1)
                                {
                                    y += CharHeight;
                                    iWrappingChar = WrappingLength;
                                    totalwraps++;
                                    linewraps++;
                                    args.DrawingSession.FillRectangle(0, y, Width_Left - Width_TextIndent, CharHeight, Color_LeftBackground);
                                }
                            }
                        }
                        else
                            for (int iChar = 0; iChar < lastChar; iChar++)
                            {
                                Char c = Lines[iLine][iChar];

                                if (c.C == '\t')
                                {
                                    x = Width_Left + CharWidth * (iChar + indents * (TabLength - 1) - charOffset);
                                    indents += 1;
                                    if (ShowControlCharacters)
                                        if (iChar >= charOffset - indents * (TabLength - 1)) // Draw indent arrows
                                        {
                                            CanvasPathBuilder pathBuilder = new(sender);

                                            pathBuilder.BeginFigure(CharWidth * 0.2f, CharHeight / 2);
                                            pathBuilder.AddLine(CharWidth * (TabLength - 0.2f), CharHeight / 2);
                                            pathBuilder.EndFigure(CanvasFigureLoop.Open);

                                            pathBuilder.BeginFigure(CharWidth * (TabLength - 0.5f), CharHeight * 1 / 4);
                                            pathBuilder.AddLine(CharWidth * (TabLength - 0.2f), CharHeight / 2);
                                            pathBuilder.AddLine(CharWidth * (TabLength - 0.5f), CharHeight * 3 / 4);
                                            pathBuilder.EndFigure(CanvasFigureLoop.Open);

                                            CanvasGeometry arrow = CanvasGeometry.CreatePath(pathBuilder);

                                            args.DrawingSession.DrawGeometry(arrow, x, y, ActualTheme == ElementTheme.Light ? Color_WeakMarker.InvertColorBrightness() : Color_WeakMarker, thickness);
                                        }
                                    if (ShowIndentGuides != IndentGuide.None)
                                    {
                                        if (iChar >= charOffset - indents * (TabLength - 1)) // Draw indent arrows
                                        {
                                            args.DrawingSession.DrawLine(x + CharWidth / 3f, y, x + CharWidth / 3f, y + CharHeight, ActualTheme == ElementTheme.Light ? Color_FoldingMarkerUnselected.InvertColorBrightness() : Color_FoldingMarkerUnselected, 1.5f, new CanvasStrokeStyle() { DashStyle = ShowIndentGuides == IndentGuide.Line ? CanvasDashStyle.Solid : CanvasDashStyle.Dash });
                                        }
                                    }
                                }
                                else if (iChar >= charOffset - indents * (TabLength - 1))
                                {
                                    if (!IsWrappingEnabled && iChar < charOffset - indents * (TabLength - 1) + visibleChars)
                                    {
                                        x = Width_Left + CharWidth * (iChar + indents * (TabLength - 1) - charOffset);
                                        if (c.T == Token.Key)
                                        {
                                            if (IsInsideBrackets(new(iChar, iLine)))
                                            {
                                                args.DrawingSession.DrawText(c.C.ToString(), x, y, ActualTheme == ElementTheme.Light ? EditorOptions.TokenColors[Token.Key].InvertColorBrightness() : EditorOptions.TokenColors[Token.Key], new CanvasTextFormat() { FontFamily = FontUri, FontSize = ScaledFontSize });
                                            }
                                            else
                                            {
                                                args.DrawingSession.DrawText(c.C.ToString(), x, y, ActualTheme == ElementTheme.Light ? EditorOptions.TokenColors[Token.Normal].InvertColorBrightness() : EditorOptions.TokenColors[Token.Normal], new CanvasTextFormat() { FontFamily = FontUri, FontSize = ScaledFontSize });
                                            }
                                        }
                                        else
                                        {
                                            args.DrawingSession.DrawText(c.C.ToString(), x, y, ActualTheme == ElementTheme.Light ? EditorOptions.TokenColors[c.T].InvertColorBrightness() : EditorOptions.TokenColors[c.T], new CanvasTextFormat() { FontFamily = FontUri, FontSize = ScaledFontSize });
                                        }
                                    }

                                    if (IsWrappingEnabled)
                                    {
                                        int maxchar = iVisibleChars;
                                        //if (iChar + indents * (TabLength - 1) - charOffset < maxchar)
                                        //{
                                        //	x = Width_Left + CharWidth * (iChar + indents * (TabLength - 1) - charOffset);
                                        //}
                                        //else
                                        //{
                                        int iWrappingEndPosition = (linewraps + 1) * maxchar - (3 * linewraps) - (indents * (TabLength - 1) * (linewraps + 1));

                                        if (iChar > 0 && iChar < iWrappingEndPosition)
                                            iWrappingChar++;

                                        if (iChar == iWrappingEndPosition)
                                        {
                                            y += CharHeight;
                                            iWrappingChar = 3;
                                            totalwraps++;
                                            linewraps++;
                                            args.DrawingSession.FillRectangle(0, y, Width_Left - Width_TextIndent, CharHeight, Color_LeftBackground);
                                            //wrapindent = 1;
                                        }

                                        //if (iChar > 0)
                                        //	if ((iChar + indents * (TabLength - 1) - charOffset) % (maxchar) == 0)
                                        //	{
                                        //		y += CharHeight;
                                        //		totalwraps++;
                                        //		linewraps++;
                                        //		wrapindent = 1;
                                        //	}
                                        x = Width_Left + CharWidth * (iWrappingChar - charOffset + indents * (TabLength - 1));
                                        //}

                                        args.DrawingSession.DrawText(c.C.ToString(), x, y, ActualTheme == ElementTheme.Light ? EditorOptions.TokenColors[c.T].InvertColorBrightness() : EditorOptions.TokenColors[c.T], new CanvasTextFormat() { FontFamily = FontUri, FontSize = ScaledFontSize });
                                    }

                                }
                            }
                        if (ShowControlCharacters && iLine < Lines.Count - 1 && lastChar >= charOffset - indents * (TabLength - 1))
                        {
                            x = Width_Left + CharWidth * (lastChar + indents * (TabLength - 1) - charOffset);
                            CanvasPathBuilder enterpath = new(sender);

                            enterpath.BeginFigure(CharWidth * 0.9f, CharHeight * 1 / 3);
                            enterpath.AddLine(CharWidth * 0.9f, CharHeight * 3 / 4);
                            enterpath.AddLine(CharWidth * 0.0f, CharHeight * 3 / 4);
                            enterpath.EndFigure(CanvasFigureLoop.Open);

                            enterpath.BeginFigure(CharWidth * 0.4f, CharHeight * 2 / 4);
                            enterpath.AddLine(CharWidth * 0.1f, CharHeight * 3 / 4);
                            enterpath.AddLine(CharWidth * 0.4f, CharHeight * 4 / 4);
                            enterpath.EndFigure(CanvasFigureLoop.Open);

                            CanvasGeometry enter = CanvasGeometry.CreatePath(enterpath);

                            args.DrawingSession.DrawGeometry(enter, x, y, ActualTheme == ElementTheme.Light ? Color_WeakMarker.InvertColorBrightness() : Color_WeakMarker, thickness);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorHandler?.Invoke(this, new ErrorEventArgs(ex));
            }
        }

        private void VerticalScrollBar_PointerEntered(object sender, PointerRoutedEventArgs e)
        {

        }

        private void VerticalScrollBar_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {

        }

        private void VerticalScrollBar_Scroll(object sender, ScrollEventArgs e)
        {

        }

        private void HorizontalScrollBar_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {

        }

        private void HorizontalScrollBar_PointerEntered(object sender, PointerRoutedEventArgs e)
        {

        }
    }
}
