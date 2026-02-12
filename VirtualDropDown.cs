using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace WinForms.VirtualDropDown;

[DefaultEvent("SelectedIndexChanged")]
public class VirtualDropDown : UserControl
{
    internal bool _isDroppedDown;
    private int _selectedIndex = -1;
    private int _virtualCount = 0;
    private string _selectedText = string.Empty;
    private const int ItemHeight = 20;
    private const int _maxDropdownHeight = 200;

    // Custom dropdown form
    private VirtualDropDownList? _dropdownForm;

    // Define the Part IDs manually since the wrappers are missing
    const int CP_READONLY = 5;
    const int CP_DROPDOWNBUTTONGLYPH = 6;

    // Events
    public event EventHandler<RetrieveVirtualItemEventArgs>? RetrieveItem;
    public event EventHandler? SelectedIndexChanged;

    public VirtualDropDown()
    {
        DoubleBuffered = true;
        Height = 23;
        BackColor = SystemColors.Window;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    // --- Public Properties ---

    [Category("Data"), Description("The number of items in the virtual list.")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ItemCount
    {
        get => _virtualCount;
        set => _virtualCount = value;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex != value)
            {
                _selectedIndex = value;
                UpdateSelectedText();
                Invalidate();
                OnSelectedIndexChanged();
            }
        }
    }

    [Browsable(false)]
    public string SelectedText
    {
        get => _selectedText;
        private set
        {
            _selectedText = value;
            Invalidate();
        }
    }

    // --- Data Handling ---

    private void UpdateSelectedText()
    {
        if (_selectedIndex > -1 && _selectedIndex < _virtualCount)
        {
            var args = new RetrieveVirtualItemEventArgs(_selectedIndex);
            OnRetrieveItem(args);
            _selectedText = args.Item?.Text ?? string.Empty;
        }
        else
        {
            _selectedText = string.Empty;
        }
    }

    protected virtual void OnRetrieveItem(RetrieveVirtualItemEventArgs e)
    {
        RetrieveItem?.Invoke(this, e);
    }

    protected virtual void OnSelectedIndexChanged()
    {
        SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
    }

    // --- Painting ---

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Draw background
        var textRect = new Rectangle(0, 0, Width - SystemInformation.VerticalScrollBarWidth, Height);
        e.Graphics.FillRectangle(new SolidBrush(BackColor), textRect);

        var state = ComboBoxState.Normal;
        if (!Enabled)
        {
            state = ComboBoxState.Disabled;
        }
        else if (_isDroppedDown)
        {
            state = ComboBoxState.Pressed;
        }
        else if (ClientRectangle.Contains(PointToClient(Cursor.Position)))
        {
            state = ComboBoxState.Hot;
        }

        if (Application.RenderWithVisualStyles && VisualStyleRenderer.IsSupported)
        {
            // 2. Draw the Unified Container (The border and background)
            // Part 5 (CP_READONLY) is the one that looks like a single white box
            var mainBox = new VisualStyleRenderer("COMBOBOX", CP_READONLY, (int)state);
            mainBox.DrawBackground(e.Graphics, ClientRectangle);

            // Draw text
            TextRenderer.DrawText(e.Graphics, _selectedText, Font, new Rectangle(1, 0, textRect.Width - 4, textRect.Height), ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

            // 3. Draw the Glyph (The Chevron Arrow)
            // Part 6 (CP_DROPDOWNBUTTONGLYPH) is just the arrow, no square box!
            var glyph = new VisualStyleRenderer("COMBOBOX", CP_DROPDOWNBUTTONGLYPH, 0);

            // Get the native size of the chevron to prevent "stretching" or clipping
            var glyphSize = glyph.GetPartSize(e.Graphics, ThemeSizeType.True);
            glyphSize.Width *= 2; //Width is doubled as windows only seems to measure one side of the chevron, but it actually needs to be drawn at double width to look correct.

            // Position it: 4 pixels from the right, vertically centered
            var glyphX = ClientRectangle.Right - glyphSize.Width - 4;
            var glyphY = (ClientRectangle.Height - glyphSize.Height) / 2;

            var glyphRect = new Rectangle(new Point(glyphX, glyphY), glyphSize);
            glyph.DrawBackground(e.Graphics, glyphRect);
        }
        else
        {
            // Draws the classic 3D sunken box
            ControlPaint.DrawBorder3D(e.Graphics, ClientRectangle, Border3DStyle.Sunken);

            // Draws the classic gray square button with the black triangle
            var btnWidth = 16;
            var btnRect = new Rectangle(Width - btnWidth, 0, btnWidth, Height);
            ControlPaint.DrawComboButton(e.Graphics, btnRect, ButtonState.Normal);

            // Draw text
            TextRenderer.DrawText(e.Graphics, _selectedText, Font, new Rectangle(1, 0, textRect.Width - 4, textRect.Height), ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }


    // --- Interaction ---

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button == MouseButtons.Left)
        {
            ToggleDropdown();
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);

        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);

        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        Height = 23;
        Invalidate();
    }

    private void ToggleDropdown()
    {
        if (_isDroppedDown)
        {
            CloseDropdown();
        }
        else
        {
            ShowDropdown();
        }
    }

    private void ShowDropdown()
    {
        int dropHeight = Math.Min(_virtualCount * ItemHeight + 4, _maxDropdownHeight);
        _dropdownForm = new VirtualDropDownList(this, (int)base.Width, dropHeight);

        // Position the dropdown below the combobox
        var dropdownPosition = PointToScreen(new Point(0, Height));
        _dropdownForm.StartPosition = FormStartPosition.Manual;
        _dropdownForm.Location = dropdownPosition;

        _dropdownForm.Show(this);
        _isDroppedDown = true;
        Invalidate();
    }

    private void CloseDropdown()
    {
        _dropdownForm?.Close();
        _dropdownForm = null;
        _isDroppedDown = false;
        Invalidate();
    }

    internal void SelectItem(int index)
    {
        SelectedIndex = index;
        CloseDropdown();
    }

    internal int GetItemCount() => _virtualCount;

    internal void OnRetrieveItemInternal(RetrieveVirtualItemEventArgs e) => OnRetrieveItem(e);
}