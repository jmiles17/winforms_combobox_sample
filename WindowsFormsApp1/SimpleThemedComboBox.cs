using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace ThemedControls
{

    /// <summary>
    /// ThemedComboBox
    /// Implement own version of ComboBox to draw it according to theming
    /// ComboBoxes are editable - DropDownStyle = DropDown/Simple
    /// DropDowns are not editable - DropDownStyle = DropDownList
    /// </summary>
    public class SimpleThemedComboBox : ComboBox
    {
        private const int WM_PAINT = 0xF;
        private const int WM_CTLCOLOREDIT = 0x0133;
        private const int WM_CTLCOLORSTATIC = 0x0138;
        private const int WM_ERASEBKGND = 0x0014;
        private bool _isHovered = false;
        private int _buttonWidth;
        private IntPtr _backgroundBrushHandle;

        /// <summary>
        /// ThemedComboBox
        /// Constructor for this class
        /// Set draw mode so that dropdown list will be drawn
        /// Get width to use for dropdown button
        /// </summary>
        public SimpleThemedComboBox()
        {
            //this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
            //        ControlStyles.AllPaintingInWmPaint |
            //        ControlStyles.UserPaint, true);
            //this.UpdateStyles();
            this.DrawMode = DrawMode.OwnerDrawFixed;
            _buttonWidth = SystemInformation.HorizontalScrollBarArrowWidth;
            //DoubleBuffered = true;
            //this.FlatStyle = FlatStyle.Flat;
        }

        /// <summary>
        /// Dispose
        /// Dispose of backgroundBrushHandle IntPtr used for WM_CTLCOLOREDIT and WM_CTLCOLORSTATIC
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_backgroundBrushHandle != IntPtr.Zero)
                {
                    DeleteObject(_backgroundBrushHandle);
                    _backgroundBrushHandle = IntPtr.Zero;
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// OnDrawItem
        /// Draw each item in the dropdown list
        /// </summary>
        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            base.OnDrawItem(e);
            Color backgroundColor;
            Color textColor;
            // get colors for item hovered/selected
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                backgroundColor = Color.DarkBlue;
                textColor = Color.White;
            }
            else
            {
                backgroundColor = Color.DarkGray;
                textColor = Color.White;
            }
            // draw background
            using (SolidBrush br = new SolidBrush(backgroundColor))
            {
                e.Graphics.FillRectangle(br, e.Bounds);
            }
            // draw text
            if (this.Items != null && this.Items.Count > 0 && e.Index >= 0)
            {
                var item = this.Items[e.Index];
                string itemText = item.ToString();
                TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter;
                TextRenderer.DrawText(e.Graphics, itemText, this.Font, e.Bounds, textColor, flags);
            }
        }

        /// <summary>
        /// WndProc
        /// Override WndProc to intercept WM_PAINT, WM_CTLCOLOREDIT, WM_CTLCOLORSTATIC
        /// WM_PAINT - paint the control
        /// WM_CTLCOLOREDIT - message sent to paint/theme the textbox editing control 
        ///   in an editable combobox
        /// WM_CTLCOLORSTATIC - message sent to paint/theme the textbox editing control 
        ///   in a disabled/readonly combobox
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_CTLCOLOREDIT || m.Msg == WM_CTLCOLORSTATIC)
            {
                IntPtr hdc = m.WParam;
                // Set back and text color
                HandleColorEditStatic(hdc);
                // get a pointer to a background brush to be returned
                _backgroundBrushHandle = CreateSolidBrush(ColorTranslator.ToWin32(BackColor));
                m.Result = _backgroundBrushHandle;
                return;
            }
            // ignore WM_ERASEBKGND to ease flickering
            if (m.Msg == WM_ERASEBKGND)
            {
                m.Result = (IntPtr)1;
                return;
            }
            base.WndProc(ref m);
            if (m.Msg == WM_PAINT)
            {
                using (PaintEventArgs pe = new PaintEventArgs(Graphics.FromHwnd(this.Handle), this.ClientRectangle))
                {
                    if (DropDownStyle == ComboBoxStyle.DropDownList)
                    {
                        DrawDropDown(pe.Graphics);
                    }
                    else
                    {
                        DrawComboBox(pe.Graphics);
                    }
                }
            }
        }

        /// <summary>
        /// DrawDropDown
        /// Draw the DropDown itself - background, border, button, and text
        /// DropDowns are comboboxes that are not editable
        /// Do not draw button and separator, unless pressed
        /// </summary>
        private void DrawDropDown(Graphics g)
        {
            //Graphics g = Graphics.FromHwnd(this.Handle);
            // set colors and standard button state
            Color backgroundColor;
            Color borderColor;
            Color foreColor;
            Color separatorColor;
            Color glyphColor;
            Color glyphBackColor;
            if (!Enabled)
            {
                backgroundColor = Color.DarkGray;
                borderColor = Color.LightGray;
                foreColor = SystemColors.GrayText;
                glyphColor = SystemColors.GrayText;
                glyphBackColor = backgroundColor;
                separatorColor = glyphBackColor;
            }
            else if (DroppedDown)
            {
                backgroundColor = Color.DarkGray;
                borderColor = Color.DarkBlue;
                foreColor = Color.White;
                glyphColor = Color.White;
                glyphBackColor = backgroundColor;
                separatorColor = glyphBackColor;
            }
            else if (_isHovered)
            {
                backgroundColor = Color.DarkGray;
                borderColor = Color.DarkBlue;
                foreColor = Color.White;
                glyphColor = Color.White;
                glyphBackColor = backgroundColor;
                separatorColor = glyphBackColor;
            }
            else
            {
                backgroundColor = Color.DarkGray;
                borderColor = Color.Blue;
                foreColor = Color.White;
                glyphColor = Color.White;
                glyphBackColor = backgroundColor;
                separatorColor = glyphBackColor;
            }
            // get rectangle for drawing
            Rectangle rect = new Rectangle(0, 0, Width, Height);
            // draw background
            using (SolidBrush br = new SolidBrush(backgroundColor))
            {
                g.FillRectangle(br, rect);
            }
            // draw text
            if (this.Items != null && this.Items.Count > 0 && this.SelectedItem != null)
            {
                TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter;
                TextRenderer.DrawText(g, SelectedItem.ToString(), this.Font, rect, foreColor, flags);
            }
            // draw dropdown button if hovered or pressed
            Rectangle dropdownRect = new Rectangle(Width - _buttonWidth, 0, _buttonWidth - 1, Height - 1);
            if (_isHovered || DroppedDown)
            {
                // draw background
                using (SolidBrush br = new SolidBrush(glyphBackColor))
                {
                    g.FillRectangle(br, dropdownRect);
                }
                // draw separator
                using (Pen p = new Pen(separatorColor))
                {
                    g.DrawLine(p, new Point(dropdownRect.Left, dropdownRect.Top), new Point(dropdownRect.Left, dropdownRect.Bottom));
                }
            }
            // draw arrow
            Point middle = new Point(dropdownRect.Left + (dropdownRect.Width / 2),
              dropdownRect.Top + (dropdownRect.Height / 2));
            Point[] arrow = new Point[]
            {
        new Point(middle.X - 3, middle.Y - 2),
        new Point(middle.X + 4, middle.Y - 2),
        new Point(middle.X, middle.Y + 2)
            };
            using (SolidBrush br = new SolidBrush(glyphColor))
            {
                g.FillPolygon(br, arrow);
            }
            // draw border last
            using (Pen p = new Pen(borderColor))
            {
                Rectangle borderRect = new Rectangle(0, 0, Width - 1, Height - 1);
                g.DrawRectangle(p, borderRect);
            }
        }

        /// <summary>
        /// DrawComboBox
        /// Draw an editable ComboBox itself - background, border, button, and text
        /// Draw separator and button explicitly
        /// Do not draw text - handled by system/DrawItem
        /// </summary>
        private void DrawComboBox(Graphics g)
        {
            //Graphics g = Graphics.FromHwnd(this.Handle);
            // set colors and standard button state
            Color backgroundColor;
            Color borderColor;
            Color foreColor;
            Color glyphColor;
            Color glyphBackColor;
            Color separatorColor;
            if (!Enabled)
            {
                backgroundColor = Color.DarkGray;
                borderColor = Color.LightGray;
                foreColor = SystemColors.GrayText;
                glyphColor = SystemColors.GrayText;
                glyphBackColor = backgroundColor;
                separatorColor = glyphBackColor;
            }
            else if (DroppedDown)
            {
                backgroundColor = Color.DarkGray;
                borderColor = Color.DarkBlue;
                foreColor = Color.White;
                glyphColor = Color.White;
                glyphBackColor = backgroundColor;
                separatorColor = glyphBackColor;
            }
            else if (_isHovered || Focused)
            {
                backgroundColor = Color.DarkGray;
                borderColor = Color.DarkBlue;
                foreColor = Color.White;
                glyphColor = Color.White;
                glyphBackColor = backgroundColor;
                separatorColor = glyphBackColor;
            }
            else
            {
                backgroundColor = Color.DarkGray;
                borderColor = Color.Blue;
                foreColor = Color.White;
                glyphColor = Color.White;
                glyphBackColor = backgroundColor;
                separatorColor = glyphBackColor;
            }
            // get rectangle for drawing
            Rectangle rect = new Rectangle(0, 0, Width, Height);
            // draw background
            using (SolidBrush br = new SolidBrush(backgroundColor))
            {
                g.FillRectangle(br, rect);
            }
            // draw text
            //if (this.Items != null && this.Items.Count > 0 && this.SelectedItem != null)
            //{
            //  TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter;
            //  TextRenderer.DrawText(g, SelectedItem.ToString(), this.Font, rect, foreColor, flags);
            //}
            // draw dropdown button
            Rectangle dropdownRect = new Rectangle(Width - _buttonWidth, 0, _buttonWidth - 1, Height - 1);
            // draw background
            using (SolidBrush br = new SolidBrush(glyphBackColor))
            {
                g.FillRectangle(br, dropdownRect);
            }
            // draw separator
            using (Pen p = new Pen(separatorColor))
            {
                g.DrawLine(p, new Point(dropdownRect.Left, dropdownRect.Top), new Point(dropdownRect.Left, dropdownRect.Bottom));
            }
            // draw arrow
            Point middle = new Point(dropdownRect.Left + (dropdownRect.Width / 2),
              dropdownRect.Top + (dropdownRect.Height / 2));
            Point[] arrow = new Point[]
            {
        new Point(middle.X - 3, middle.Y - 2),
        new Point(middle.X + 4, middle.Y - 2),
        new Point(middle.X, middle.Y + 2)
            };
            using (SolidBrush br = new SolidBrush(glyphColor))
            {
                g.FillPolygon(br, arrow);
            }
            // draw border last
            using (Pen p = new Pen(borderColor))
            {
                Rectangle borderRect = new Rectangle(0, 0, Width - 1, Height - 1);
                g.DrawRectangle(p, borderRect);
            }
        }

        /// <summary>
        /// HandleColorEditStatic
        /// Set a text and back color to be used for the editing control in response to WM_CTLCOLOREDIT and WM_CTLCOLORSTATIC
        /// </summary>
        private void HandleColorEditStatic(IntPtr handle)
        {
            if (!Enabled)
            {
                BackColor = Color.DarkGray;
                ForeColor = SystemColors.GrayText;
            }
            else if (DroppedDown)
            {
                BackColor = Color.DarkGray;
                ForeColor = Color.White;
            }
            else if (_isHovered || Focused)
            {
                BackColor = Color.DarkGray;
                ForeColor = Color.White;
            }
            else
            {
                BackColor = Color.DarkGray;
                ForeColor = Color.White;
            }
            SetTextColor(handle, ColorTranslator.ToWin32(ForeColor));
            SetBkColor(handle, ColorTranslator.ToWin32(BackColor));
        }

        // override mouse enter and leave to set isHovered
        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            base.OnMouseEnter(e);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            base.OnMouseLeave(e);
        }

        //protected override CreateParams CreateParams
        //{
        //  get
        //  {
        //    CreateParams cp = base.CreateParams;
        //    cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
        //    return cp;
        //  }
        //}

        /*
         * Import Win32 functions to resolve WM_CTLCOLOREDIT and WM_CTLCOLORSTATIC
         */
        [DllImport("gdi32.dll")]
        private static extern int SetTextColor(IntPtr hdc, int color);

        [DllImport("gdi32.dll")]
        private static extern int SetBkColor(IntPtr hdc, int color);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateSolidBrush(int color);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

    }

}

