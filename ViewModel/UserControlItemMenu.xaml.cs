using System.Windows;
using System.Windows.Controls;

namespace TrueMiningDesktop.ViewModel
{
    /// <summary>
    /// Interação lógica para UserControlItemMenu.xam
    /// </summary>
    public partial class UserControlItemMenu : UserControl
    {
        private MainWindow _context;

        public UserControlItemMenu(ItemMenu itemMenu, MainWindow context)
        {
            InitializeComponent();

            _context = context;

            ExpanderMenu.Visibility = itemMenu.SubItems == null ? Visibility.Collapsed : Visibility.Visible;
            ListViewItemMenu.Visibility = itemMenu.SubItems == null ? Visibility.Visible : Visibility.Collapsed;

            this.DataContext = itemMenu;

            Screen = itemMenu.Screen;
        }

        private void ListViewMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _context.SwitchScreen(((SubItem)((ListView)sender).SelectedItem).Screen);
            _context.MenuMenu.ScrollIntoView(_context.MenuMenu.Items[0]); //scroll to top
        }

        public UserControl Screen { get; private set; }

        private void UserControl_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ListViewMenu.SelectedIndex >= 0)
                _context.SwitchScreen(((SubItem)((ListView)this.ListViewMenu).SelectedItem).Screen);
        }
    }
}