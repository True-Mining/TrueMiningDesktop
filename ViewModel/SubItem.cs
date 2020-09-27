using System.Windows.Controls;

namespace True_Mining_v4.ViewModel
{
    public class SubItem
    {
        public SubItem(string name, UserControl screen = null)
        {
            Name = name;
            Screen = screen;
        }

        public string Name { get; private set; }
        public UserControl Screen { get; private set; }
    }
}