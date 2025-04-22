using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;

namespace ThemedControls
{

    /// <summary>
    /// ThemedComboBox
    /// Implement own version of ComboBox to draw it according to theming
    /// ComboBoxes are editable - DropDownStyle = DropDown/Simple
    /// DropDowns are not editable - DropDownStyle = DropDownList
    /// </summary>
    public class ThemedComboBox : ComboBox
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
        public ThemedComboBox()
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
                backgroundColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxListItemBackgroundHover);
                textColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxListItemTextHover);
            }
            else
            {
                backgroundColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxListItemBackground);
                textColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxListItemText);
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
                backgroundColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownBackgroundDisabled);
                borderColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownBorderDisabled);
                //foreColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownTextDisabled);
                // fix bug where disabled text did not appear disabled in non-default themes
                // use default disabled text color - works well enough
                foreColor = SystemColors.GrayText;
                glyphColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownGlyphDisabled);

                glyphBackColor = backgroundColor;
                separatorColor = glyphBackColor;
            }
            else if (DroppedDown)
            {
                backgroundColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownBackgroundPressed);
                borderColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownBorderPressed);
                foreColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownTextPressed);
                glyphColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownGlyphPressed);
                glyphBackColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownGlyphBackgroundPressed);
                separatorColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownSeparatorPressed);
            }
            else if (_isHovered)
            {
                backgroundColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownBackgroundHover);
                borderColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownBorderHover);
                foreColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownTextHover);
                glyphColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownGlyphHover);
                glyphBackColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownGlyphBackgroundHover);
                separatorColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownSeparatorHover);
            }
            else
            {
                backgroundColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownBackground);
                borderColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownBorder);
                foreColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownText);
                glyphColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.DropDownGlyph);

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
                backgroundColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxBackgroundDisabled);
                borderColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxBorderDisabled);
                foreColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxTextDisabled);
                glyphColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxGlyphDisabled);
                glyphBackColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxGlyphBackgroundDisabled);
                separatorColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxSeparatorDisabled);
            }
            else if (DroppedDown)
            {
                backgroundColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxBackgroundPressed);
                borderColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxBorderPressed);
                foreColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxTextPressed);
                glyphColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxGlyphPressed);
                glyphBackColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxGlyphBackgroundPressed);
                separatorColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxSeparatorPressed);
            }
            else if (Focused)
            {
                backgroundColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxBackgroundFocused);
                borderColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxBorderFocused);
                foreColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxTextFocused);
                glyphColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxGlyphFocused);
                glyphBackColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxGlyphBackgroundFocused);
                separatorColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxSeparatorFocused);
            }
            else if (_isHovered)
            {
                backgroundColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxBackgroundHover);
                borderColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxBorderHover);
                foreColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxTextHover);
                glyphColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxGlyphHover);
                glyphBackColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxGlyphBackgroundHover);
                separatorColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxSeparatorHover);
            }
            else
            {
                backgroundColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxBackground);
                borderColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxBorder);
                foreColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxText);
                glyphColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxGlyph);
                glyphBackColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxGlyphBackground);
                separatorColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxSeparator);
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
                BackColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxBackgroundDisabled);
                ForeColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxTextDisabled);
            }
            else if (Focused)
            {
                BackColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxBackgroundFocused);
                ForeColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxTextFocused);
            }
            else if (_isHovered)
            {
                BackColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxBackgroundHover);
                ForeColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxTextHover);
            }
            else if (DroppedDown)
            {
                BackColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxBackgroundPressed);
                ForeColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxTextPressed);
            }
            else
            {
                BackColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxBackground);
                ForeColor = ThemedComboBoxHelper.GetThemedColor(ThemedComboBoxHelper.ThemedControlItem.ComboBoxText);
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



    public static class ThemedComboBoxHelper
    {

        internal static Color GetThemedColor(ThemedControlItem item)
        {
            return VSColorTheme.GetThemedColor(_controlColorKeyDict[item]);
        }

        /// <summary>
        /// _controlColorKeyDict
        /// Store a dictionary that gets a color resource key for control items
        /// </summary>
        private static Dictionary<ThemedControlItem, ThemeResourceKey> _controlColorKeyDict =
          new Dictionary<ThemedControlItem, ThemeResourceKey>
          {
          { ThemedControlItem.ComboBoxBackground , CommonControlsColors.ComboBoxBackgroundColorKey },
        { ThemedControlItem.ComboBoxBackgroundDisabled , CommonControlsColors.ComboBoxBackgroundDisabledColorKey },
        { ThemedControlItem.ComboBoxBackgroundFocused , CommonControlsColors.ComboBoxBackgroundFocusedColorKey },
        { ThemedControlItem.ComboBoxBackgroundHover , CommonControlsColors.ComboBoxBackgroundHoverColorKey },
        { ThemedControlItem.ComboBoxBackgroundPressed , CommonControlsColors.ComboBoxBackgroundPressedColorKey },

        { ThemedControlItem.ComboBoxBorder , CommonControlsColors.ComboBoxBorderColorKey },
        { ThemedControlItem.ComboBoxBorderDisabled , CommonControlsColors.ComboBoxBorderDisabledColorKey },
        { ThemedControlItem.ComboBoxBorderFocused , CommonControlsColors.ComboBoxBorderFocusedColorKey },
        { ThemedControlItem.ComboBoxBorderHover , CommonControlsColors.ComboBoxBorderHoverColorKey },
        { ThemedControlItem.ComboBoxBorderPressed , CommonControlsColors.ComboBoxBorderPressedColorKey },

        { ThemedControlItem.ComboBoxGlyph , CommonControlsColors.ComboBoxGlyphColorKey },
        { ThemedControlItem.ComboBoxGlyphDisabled , CommonControlsColors.ComboBoxGlyphDisabledColorKey },
        { ThemedControlItem.ComboBoxGlyphFocused , CommonControlsColors.ComboBoxGlyphFocusedColorKey },
        { ThemedControlItem.ComboBoxGlyphHover , CommonControlsColors.ComboBoxGlyphHoverColorKey },
        { ThemedControlItem.ComboBoxGlyphPressed , CommonControlsColors.ComboBoxGlyphPressedColorKey },

        { ThemedControlItem.ComboBoxGlyphBackground , CommonControlsColors.ComboBoxGlyphBackgroundColorKey },
        { ThemedControlItem.ComboBoxGlyphBackgroundDisabled , CommonControlsColors.ComboBoxGlyphBackgroundDisabledColorKey },
        { ThemedControlItem.ComboBoxGlyphBackgroundFocused , CommonControlsColors.ComboBoxGlyphBackgroundFocusedColorKey },
        { ThemedControlItem.ComboBoxGlyphBackgroundHover , CommonControlsColors.ComboBoxGlyphBackgroundHoverColorKey },
        { ThemedControlItem.ComboBoxGlyphBackgroundPressed , CommonControlsColors.ComboBoxGlyphBackgroundPressedColorKey },

        { ThemedControlItem.ComboBoxSeparator , CommonControlsColors.ComboBoxSeparatorColorKey },
        { ThemedControlItem.ComboBoxSeparatorDisabled , CommonControlsColors.ComboBoxSeparatorDisabledColorKey },
        { ThemedControlItem.ComboBoxSeparatorFocused , CommonControlsColors.ComboBoxSeparatorFocusedColorKey },
        { ThemedControlItem.ComboBoxSeparatorHover , CommonControlsColors.ComboBoxSeparatorHoverColorKey },
        { ThemedControlItem.ComboBoxSeparatorPressed , CommonControlsColors.ComboBoxSeparatorPressedColorKey },

        { ThemedControlItem.ComboBoxText , CommonControlsColors.ComboBoxTextColorKey },
        { ThemedControlItem.ComboBoxTextDisabled , CommonControlsColors.ComboBoxTextDisabledColorKey },
        { ThemedControlItem.ComboBoxTextFocused , CommonControlsColors.ComboBoxTextFocusedColorKey },
        { ThemedControlItem.ComboBoxTextHover , CommonControlsColors.ComboBoxTextHoverColorKey },
        { ThemedControlItem.ComboBoxTextPressed , CommonControlsColors.ComboBoxTextPressedColorKey },

        { ThemedControlItem.DropDownBackground , EnvironmentColors.DropDownBackgroundColorKey },
        { ThemedControlItem.DropDownBackgroundDisabled , EnvironmentColors.DropDownDisabledBackgroundColorKey },
        { ThemedControlItem.DropDownBackgroundHover , EnvironmentColors.DropDownMouseOverBackgroundEndColorKey },
        { ThemedControlItem.DropDownBackgroundPressed , EnvironmentColors.DropDownMouseDownBackgroundColorKey },

        { ThemedControlItem.DropDownBorder , EnvironmentColors.DropDownBorderColorKey },
        { ThemedControlItem.DropDownBorderDisabled , EnvironmentColors.DropDownDisabledBorderColorKey },
        { ThemedControlItem.DropDownBorderHover , EnvironmentColors.DropDownMouseOverBorderColorKey },
        { ThemedControlItem.DropDownBorderPressed , EnvironmentColors.DropDownMouseDownBorderColorKey },

        { ThemedControlItem.DropDownGlyph , EnvironmentColors.DropDownGlyphColorKey },
        { ThemedControlItem.DropDownGlyphDisabled , EnvironmentColors.DropDownDisabledGlyphColorKey },
        { ThemedControlItem.DropDownGlyphHover , EnvironmentColors.DropDownMouseOverGlyphColorKey },
        { ThemedControlItem.DropDownGlyphPressed , EnvironmentColors.DropDownMouseDownGlyphColorKey },

        { ThemedControlItem.DropDownGlyphBackgroundHover , EnvironmentColors.DropDownButtonMouseOverBackgroundColorKey },
        { ThemedControlItem.DropDownGlyphBackgroundPressed , EnvironmentColors.DropDownButtonMouseDownBackgroundColorKey },

        { ThemedControlItem.DropDownSeparatorHover , EnvironmentColors.DropDownButtonMouseOverSeparatorColorKey },
        { ThemedControlItem.DropDownSeparatorPressed , EnvironmentColors.DropDownButtonMouseDownSeparatorColorKey },

        { ThemedControlItem.DropDownText , EnvironmentColors.DropDownTextColorKey },
        { ThemedControlItem.DropDownTextDisabled , EnvironmentColors.DropDownDisabledTextColorKey },
        { ThemedControlItem.DropDownTextHover , EnvironmentColors.DropDownMouseOverTextColorKey },
        { ThemedControlItem.DropDownTextPressed , EnvironmentColors.DropDownMouseDownTextColorKey },

        { ThemedControlItem.ComboBoxListItemBackground , EnvironmentColors.DropDownPopupBackgroundBeginColorKey },
        { ThemedControlItem.ComboBoxListItemBackgroundHover , EnvironmentColors.ComboBoxItemMouseOverBackgroundColorKey },
        { ThemedControlItem.ComboBoxListItemText , EnvironmentColors.ComboBoxItemTextColorKey },
        { ThemedControlItem.ComboBoxListItemTextHover , EnvironmentColors.ComboBoxItemMouseOverTextColorKey },
        { ThemedControlItem.ComboBoxListItemBorder , EnvironmentColors.DropDownPopupBorderColorKey },
        { ThemedControlItem.ComboBoxListItemBorderHover , EnvironmentColors.ComboBoxItemMouseOverBorderColorKey },
          };



    public enum ThemedControlItem
        {
            ComboBoxBackground,
            ComboBoxBackgroundDisabled,
            ComboBoxBackgroundFocused,
            ComboBoxBackgroundHover,
            ComboBoxBackgroundPressed,
            ComboBoxBorder,
            ComboBoxBorderDisabled,
            ComboBoxBorderFocused,
            ComboBoxBorderHover,
            ComboBoxBorderPressed,
            ComboBoxGlyph,
            ComboBoxGlyphDisabled,
            ComboBoxGlyphFocused,
            ComboBoxGlyphHover,
            ComboBoxGlyphPressed,
            ComboBoxGlyphBackground,
            ComboBoxGlyphBackgroundDisabled,
            ComboBoxGlyphBackgroundFocused,
            ComboBoxGlyphBackgroundHover,
            ComboBoxGlyphBackgroundPressed,
            ComboBoxSeparator,
            ComboBoxSeparatorDisabled,
            ComboBoxSeparatorFocused,
            ComboBoxSeparatorHover,
            ComboBoxSeparatorPressed,
            ComboBoxText,
            ComboBoxTextDisabled,
            ComboBoxTextFocused,
            ComboBoxTextHover,
            ComboBoxTextPressed,
            ComboBoxListItemBackground,
            ComboBoxListItemBackgroundHover,
            ComboBoxListItemText,
            ComboBoxListItemTextHover,
            ComboBoxListItemBorder,
            ComboBoxListItemBorderHover,
            DropDownBackground,
            DropDownBackgroundDisabled,
            DropDownBackgroundFocused,
            DropDownBackgroundHover,
            DropDownBackgroundPressed,
            DropDownBorder,
            DropDownBorderDisabled,
            DropDownBorderFocused,
            DropDownBorderHover,
            DropDownBorderPressed,
            DropDownGlyph,
            DropDownGlyphDisabled,
            DropDownGlyphFocused,
            DropDownGlyphHover,
            DropDownGlyphPressed,
            DropDownGlyphBackground,
            DropDownGlyphBackgroundDisabled,
            DropDownGlyphBackgroundFocused,
            DropDownGlyphBackgroundHover,
            DropDownGlyphBackgroundPressed,
            DropDownSeparator,
            DropDownSeparatorDisabled,
            DropDownSeparatorFocused,
            DropDownSeparatorHover,
            DropDownSeparatorPressed,
            DropDownText,
            DropDownTextDisabled,
            DropDownTextFocused,
            DropDownTextHover,
            DropDownTextPressed,
        }

    }

}