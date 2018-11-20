using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Input;
using System.Collections;
using ChessLib;
using System.Windows.Data;
using Microsoft.Win32;

namespace Zebra
{
    static class EventToCommand
    {
        // The EventToCommand.MakeMoveCommand attached property is applied to ListboxItems in order to execute an ICommand on the MouseDown event
        public static readonly DependencyProperty PreviewMouseDownProperty = DependencyProperty.RegisterAttached
            ("PreviewMouseDown", typeof(ICommand), typeof(EventToCommand), new PropertyMetadata(previewMouseDownChanged));

        public static ICommand GetPreviewMouseDown(ListBoxItem item)
        {
            if (item == null)
                throw new ArgumentException("item");
            return (ICommand)item.GetValue(PreviewMouseDownProperty);
        }

        public static void SetPreviewMouseDown(ListBoxItem item, ICommand command)
        {
            if (item == null)
                throw new ArgumentException("item");
            item.SetValue(PreviewMouseDownProperty, command);
        }

        static void previewMouseDownChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ListBoxItem item = o as ListBoxItem;
            if (item != null)
            {
                if (e.NewValue != null && e.OldValue == null)
                    item.PreviewMouseDown += item_PreviewMouseDown;
                else if (e.NewValue == null && e.OldValue != null)
                    item.PreviewMouseDown -= item_PreviewMouseDown;
            }
            else
                throw new InvalidOperationException("The attached PreviewMouseDownCommand property can only be applied to ListBoxItem instances.");
        }

        static void item_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ListBoxItem item = (ListBoxItem)sender;
            // Either of the next two lines will work
            // ICommand command = (ICommand)box.GetValue(SelectionChangedCommandProperty);
            ICommand command = EventToCommand.GetPreviewMouseDown(item);
            ChessMove move = (ChessMove)item.Content;
            command.Execute(move);
        }

        // Using a DependencyProperty as the backing store for PlyUpdated.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PlyUpdatedProperty =
            DependencyProperty.RegisterAttached("PlyUpdated", typeof(ICommand), typeof(EventToCommand), new PropertyMetadata(plyUpdatedChanged));

        public static ICommand GetPlyUpdated(ListBox box)
        {
            if (box == null)
                throw new ArgumentException("item");
            return (ICommand)box.GetValue(PlyUpdatedProperty);
        }

        public static void SetPlyUpdated(DependencyObject box, ICommand value)
        {
            if (box == null)
                throw new ArgumentException("item");
            box.SetValue(PlyUpdatedProperty, value);
        }

        static void plyUpdatedChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ListBox box = o as ListBox;
            if (o != null)
            {
                if (e.NewValue != null && e.OldValue == null)
                    box.SelectionChanged += new SelectionChangedEventHandler(box_SelectionChanged);
                if (e.NewValue == null && e.OldValue != null)
                    box.SelectionChanged -= new SelectionChangedEventHandler(box_SelectionChanged);
            }
        }

        static void box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox box = (ListBox)sender;
            int plyNr = box.SelectedIndex;
            if (e.AddedItems.Count > 0)
                box.ScrollIntoView(e.AddedItems[0]);
            ICommand command = (ICommand)box.GetValue(PlyUpdatedProperty);
            command.Execute(plyNr);
        }
    }

    static class Behavior
    {
        #region SaveCommandProperty
        static readonly DependencyProperty SaveCommandProperty = DependencyProperty.RegisterAttached("SaveCommand", typeof(ICommand), typeof(Behavior), new PropertyMetadata(saveCommandChanged));

        public static ICommand GetSaveCommand(UIElement element)
        {
            if (element == null)
                throw new ArgumentException("element");
            return (ICommand)element.GetValue(SaveCommandProperty);
        }

        public static void SetSaveCommand(UIElement element, ICommand saveCommand)
        {
            if (element == null)
                throw new ArgumentException("element");
            element.SetValue(SaveCommandProperty, saveCommand);
        }

        static void saveCommandChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            MenuItem element = o as MenuItem;
            if (element != null)
            {
                if (e.NewValue != null && e.OldValue == null)
                    element.Click += new RoutedEventHandler(element_Click);
                if (e.NewValue == null && e.OldValue != null)
                    element.Click -= new RoutedEventHandler(element_Click);
            }
        }

        static void element_Click(object sender, RoutedEventArgs e)
        {
            MenuItem element = sender as MenuItem;
            ICommand command = (ICommand)element.GetValue(SaveCommandProperty);
            string filename;
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = "pgn";
            dialog.Filter = "Portable Game Notation (.pgn)|*.pgn";
            if (dialog.ShowDialog() == true)
            {
                filename = dialog.FileName;
                command.Execute(filename);
            }
        }
        #endregion

        #region DragCommandProperty
        public static readonly DependencyProperty DragCommandProperty = DependencyProperty.RegisterAttached
            ("DragCommand", typeof(ICommand), typeof(Behavior), new PropertyMetadata(dragCommandChanged));

        static Point lastMousePosition;
        static FrameworkElement element;
        static double X, Y;

        public static ICommand GetDragCommand(UIElement element)
        {
            if (element == null)
                throw new ArgumentException("element");
            return (ICommand)element.GetValue(DragCommandProperty);
        }

        public static void SetDragCommand(UIElement element, ICommand dragCommand)
        {
            if (element == null)
                throw new ArgumentException("element");
            element.SetValue(DragCommandProperty, dragCommand);
        }

        static void dragCommandChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            UIElement element = o as UIElement;
            //if (!(element is Image)) throw new InvalidOperationException("Can only apply the DragCommand behavior to Images.");
            if (element != null)
            {
                if (e.NewValue != null && e.OldValue == null)
                    element.PreviewMouseDown += element_PreviewMouseDown;
                else if (e.NewValue == null && e.OldValue != null)
                    element.PreviewMouseDown -= element_PreviewMouseDown;
            }
        }

        static void element_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            element = (FrameworkElement)sender;
            Canvas canvas = VisualTreeHelperExtensions.FindAncestor<Canvas>(element);
            if (canvas == null) return;
            canvas.PreviewMouseMove += canvas_PreviewMouseMove;
            canvas.PreviewMouseUp += canvas_PreviewMouseUp;
            lastMousePosition = e.GetPosition(canvas);
            Canvas.SetZIndex(element, 1);
            canvas.CaptureMouse();
            X = 0; Y = 0;
            element.RenderTransform = new TranslateTransform(X, Y);
        }

        static void canvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Canvas canvas = (Canvas)sender;
            Point newMousePosition = e.GetPosition(canvas);
            double deltaX = newMousePosition.X - lastMousePosition.X;
            double deltaY = newMousePosition.Y - lastMousePosition.Y;
            lastMousePosition = newMousePosition;
            X += deltaX; Y += deltaY;
            element.RenderTransform = new TranslateTransform(X, Y);
        }

        static void canvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Canvas canvas = (Canvas)sender;
            canvas.PreviewMouseMove -= canvas_PreviewMouseMove;
            canvas.PreviewMouseUp -= canvas_PreviewMouseUp;
            element.RenderTransform = null;
            Canvas.SetZIndex(element, 0);
            canvas.ReleaseMouseCapture();
            int fromFile = ChessGrid.GetFile(element); int fromRank = ChessGrid.GetRank(element);
            Point p = e.GetPosition(canvas);
            int toFile = (int)(p.X / canvas.ActualWidth * 8);
            int toRank = 7-(int)(p.Y / canvas.ActualHeight * 8);
            if (GetIsFlipped(canvas))
            {
                toFile = 7 - toFile;
                toRank = 7 - toRank;
            }
            ICommand command = Behavior.GetDragCommand(element);
            ChessPiece piece = (ChessPiece)element.DataContext;
            command.Execute(new object[] { piece, toFile, toRank });
        }
        #endregion

        #region IsFlippedProperty
        // IsFlipped is used for when white is on top of the chessboard
        public static readonly DependencyProperty IsFlippedProperty = DependencyProperty.RegisterAttached
            ("IsFlipped", typeof(bool), typeof(Behavior), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        public static bool GetIsFlipped(UIElement element)
        {
            if (element == null)
                throw new ArgumentException("element");
            return (bool)element.GetValue(IsFlippedProperty);
        }

        public static void SetIsFlipped(UIElement element, bool isFlipped)
        {
            if (element == null)
                throw new ArgumentException("element");
            element.SetValue(IsFlippedProperty, isFlipped);
        }
        #endregion

        #region ScrollOnTextChangedProperty
        public static readonly DependencyProperty ScrollOnTextChangedProperty = DependencyProperty.RegisterAttached
            ("ScrollOnTextChanged", typeof(bool), typeof(Behavior), new PropertyMetadata(false, ScrollOnTextChangedChanged));

        private static void ScrollOnTextChangedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb != null)
            {
                if (e.NewValue != null && (bool)e.NewValue)
                    tb.TextChanged += TextChanged;
                else
                    tb.TextChanged -= TextChanged;
            }
            else
                throw new InvalidOperationException("The attached ScrollOnTextChanged property can only be applied to TextBox instances.");
        }

        public static bool GetScrollOnTextChanged(TextBox textBox)
        {
            if (textBox == null)
                throw new ArgumentNullException("textBox");
            return (bool)textBox.GetValue(ScrollOnTextChangedProperty);
        }

        public static void SetScrollOnTextChanged(TextBox textBox, bool scrollOnTextChanged)
        {
            if (textBox == null)
                throw new ArgumentNullException("textBox");
            textBox.SetValue(ScrollOnTextChangedProperty, scrollOnTextChanged);
        }

        private static void TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox box = (TextBox)sender;
            box.UpdateLayout();
            var viewer = box.GetChildByType<ScrollViewer>(item => true);
            if (viewer != null)
                viewer.ScrollToVerticalOffset(viewer.ScrollableHeight);
        }
        #endregion
    }

    static class ChessGrid
    {
        static readonly DependencyProperty FileProperty = DependencyProperty.RegisterAttached
            ("File", typeof(int), typeof(ChessGrid));
        static readonly DependencyProperty RankProperty = DependencyProperty.RegisterAttached
            ("Rank", typeof(int), typeof(ChessGrid));

        public static int GetFile(UIElement element)
        {
            if (element == null)
                throw new ArgumentException("element");
            return (int)element.GetValue(FileProperty);
        }

        public static void SetFile(UIElement element, int file)
        {
            if (element == null)
                throw new ArgumentException("element");
            element.SetValue(FileProperty, file);
        }

        public static int GetRank(UIElement element)
        {
            if (element == null)
                throw new ArgumentException("element");
            return (int)element.GetValue(RankProperty);
        }

        public static void SetRank(UIElement element, int rank)
        {
            if (element == null)
                throw new ArgumentException("element");
            element.SetValue(RankProperty, rank);
        }
    }

    public static class VisualTreeHelperExtensions
    {
        public static T FindAncestor<T>(DependencyObject dependencyObject)
            where T : class
        {
            DependencyObject target = dependencyObject;
            do
            {
                target = VisualTreeHelper.GetParent(target);
            }
            while (target != null && !(target is T));
            return target as T;
        }
    }

    public static class Extensions
    {
        public static T GetChildByType<T>(this UIElement element, Func<T, bool> condition)
            where T : UIElement
        {
            List<T> results = new List<T>();
            GetChildrenByType<T>(element, condition, results);
            if (results.Count > 0)
                return results[0];
            else
                return null;
        }

        private static void GetChildrenByType<T>(UIElement element, Func<T, bool> condition, List<T> results)
            where T : UIElement
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                UIElement child = VisualTreeHelper.GetChild(element, i) as UIElement;
                if (child != null)
                {
                    T t = child as T;
                    if (t != null)
                    {
                        if (condition == null)
                            results.Add(t);
                        else if (condition(t))
                            results.Add(t);
                    }
                    GetChildrenByType<T>(child, condition, results);
                }
            }
        }
    }
}
