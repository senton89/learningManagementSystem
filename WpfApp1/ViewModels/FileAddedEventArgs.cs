using System;
using WpfApp1.Models;

namespace WpfApp1.ViewModels
{
    public class FileAddedEventArgs : EventArgs
    {
        public FileInfo File { get; }

        public FileAddedEventArgs(FileInfo file)
        {
            File = file;
        }
    }

    public class FileRemovedEventArgs : EventArgs
    {
        public FileInfo File { get; }

        public FileRemovedEventArgs(FileInfo file)
        {
            File = file;
        }
    }
}