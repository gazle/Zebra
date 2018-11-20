using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Zebra
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ViewModel viewModel;

        public MainWindow()
        {
            DataContextChanged += new DependencyPropertyChangedEventHandler(MainWindow_DataContextChanged);
            InitializeComponent();
        }

        void MainWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            viewModel = (ViewModel)e.NewValue;
        }

        private void ListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            ListBox box = (ListBox)sender;
            if (e.Key == Key.Left)
            {
                viewModel.UndoMoveCommand.Execute(null);
                e.Handled = true;
            }
            if (e.Key == Key.Right)
            {
                viewModel.RemakeMoveCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
