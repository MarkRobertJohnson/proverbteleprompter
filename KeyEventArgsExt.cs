using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ProverbTeleprompter
{
    public class KeyEventArgsExt : KeyEventArgs
    {
        public KeyEventArgsExt(Keys keyData) : base(keyData)
        {
        }

        public KeyEventArgsExt(Keys keyData, bool alt, bool shift, bool control) : base(keyData)
        {
            
            _alt = alt;
            _shift = shift;
            _control = control;
        }

        private bool _alt;
        public override bool Alt 
        { 
            get
            {
                return _alt;

            } 
        }

        private bool _shift;
        public override bool Shift
        {
            get
            {
                return _shift;
            }
        }

        private bool _control;
        public new bool Control
        {
            get
            {
                return _control;
            }
        }
    }
}
