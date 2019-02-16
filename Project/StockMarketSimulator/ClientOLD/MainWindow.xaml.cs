﻿using CommSystem;
using System;
using System.ComponentModel;
using System.Windows;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        private int _port;
        public int Port
        {
            get => _port; set
            {
                if (Port != value)
                {
                    _port = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Port"));
                }
            }

        }

        private HelloWorld helloWorld = new HelloWorld();
        private string helloTextLocal;
        public string HelloTextLocal
        {
            get => helloTextLocal;
            set
            {
                if (helloTextLocal != value)
                {
                    helloTextLocal = value;
                    helloWorld.HelloText = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("HelloTextLocal"));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            helloWorld.HelloTextChanged += OnHelloTextChanged;

            HelloTextLocal = helloWorld.HelloText;
        }

        public void OnHelloTextChanged(object source, EventArgs args)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(HelloTextLocal));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
