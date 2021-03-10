using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dofus.Retro.Supertools.Windows
{
    /// <summary>
    /// Interaction logic for CharactersPositionLocator.xaml
    /// </summary>
    public partial class CharactersPositionLocator : Window
    {
        public List<Border> Borders { get; set; }

        public CharactersPositionLocator()
        {
            InitializeComponent();
            Borders = GrdBorder.Children.OfType<Border>().ToList();
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
