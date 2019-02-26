using System;
using System.Windows;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace YUVTube {
    public partial class SizeChanger : Window {
        public int width = 0;
        public int height = 0;
        public SizeChanger(int width, int height) {
            InitializeComponent();
            this.width = width;
            this.height = height;
            w.Text = width.ToString();
            h.Text = height.ToString();
        }
        private void OnlyNumbers(object sender, TextCompositionEventArgs e) { e.Handled = (new Regex("[^0-9]+")).IsMatch(e.Text); }
        private void Button_Click(object sender, RoutedEventArgs e) {
            width = w.Text != "" && Int32.Parse(w.Text) > 0 ? Int32.Parse(w.Text) : 1;
            height = h.Text != "" && Int32.Parse(h.Text) > 0 ? Int32.Parse(h.Text) : 1;
            this.Close();
        }
    }
}