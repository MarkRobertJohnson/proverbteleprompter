using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;

namespace ProverbTeleprompter
{
    internal class Bookmark : NotifyPropertyChangedBase
    {
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value;

                if(Hyperlink != null)
                {
                    Hyperlink.NavigateUri = new Uri(String.Format("http://bookmark/{0}", _name));
                }
            
                Changed(() => Name);
            }
        }

        private double _topOffset;
        public double TopOffset
        {
            get { return _topOffset; }
            set
            {
                _topOffset = value;
                Changed(() => TopOffset);
            }
        }

        private int _line;
        public int Line
        {
            get { return _line; }
            set
            {
                _line = value;
                Changed(() => Line);
            }
        }

        private int _ordinal;
        public int Ordinal
        {
            get { return _ordinal; }
            set
            {
                Changed(() => Ordinal);
                _ordinal = value;
            }
        }


        [DependsUpon("Name")]
        [DependsUpon("Ordinal")]
        public string DisplayTitle { get
        {
            return string.Format("{0}: {1}",Ordinal, Name);
        } }



        private Hyperlink _hyperlink;
        public Hyperlink Hyperlink
        {
            get { return _hyperlink; }
            set
            {
                _hyperlink = value;
                Changed(() => Hyperlink);
            }
        }

        private TextPointer _position;
        public TextPointer Position
        {
            get { return _position; }
            set
            {
                _position = value;
                Changed(() => Position);
            }
        }

        private Image _image;
        public Image Image
        {
            get { return _image; }
            set
            {
                _image = value;
                Changed(() => Image);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
