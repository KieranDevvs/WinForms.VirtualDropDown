namespace WinForms.VirtualDropDown;

public class VirtualDropDownList : Form
{
    private readonly VirtualDropDown _parent;
    private int _hoveredIndex = -1;
    private int _scrollOffset = 0;
    private const int ItemHeight = 20;
    private readonly VScrollBar _scrollBar;

    public VirtualDropDownList(VirtualDropDown parent, int width, int height)
    {
        _parent = parent;

        FormBorderStyle = FormBorderStyle.None;
        Width = width;
        Height = height;
        BackColor = SystemColors.Window;
        DoubleBuffered = true;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;

        // Create and configure scrollbar (manually positioned, not docked)
        _scrollBar = new VScrollBar
        {
            SmallChange = 1,
            LargeChange = (height - 4) / ItemHeight
        };
        _scrollBar.ValueChanged += OnScrollBarValueChanged;
        Controls.Add(_scrollBar);

        // Position scrollbar: width=16, right side, below the border
        var scrollBarWidth = 16;
        _scrollBar.Left = width - scrollBarWidth - 1;
        _scrollBar.Top = 1;
        _scrollBar.Width = scrollBarWidth;
        _scrollBar.Height = height - 2;

        MouseWheel += OnMouseWheel;
        MouseMove += OnMouseMove;
        MouseClick += OnMouseClick;
        Deactivate += OnDeactivate; // Close dropdown when it loses focus
    }

    private void OnScrollBarValueChanged(object? sender, EventArgs e)
    {
        // Clamp scroll offset to valid range so the last items are always visible
        var totalItems = _parent.GetItemCount();
        var visibleItems = (Height - 2 + ItemHeight - 1) / ItemHeight;

        _scrollOffset = Math.Min(_scrollBar.Value, Math.Max(0, totalItems - visibleItems));
        Invalidate();
    }

    private void OnDeactivate(object? sender, EventArgs e)
    {
        // Close the dropdown when it loses focus
        Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Update scrollbar properties based on item count
        var totalItems = _parent.GetItemCount();

        // Calculate visible items using ceiling division to fill the space including partial items
        var visibleItems = (Height - 2 + ItemHeight - 1) / ItemHeight;

        if (totalItems > visibleItems)
        {
            _scrollBar.Maximum = totalItems;
            _scrollBar.LargeChange = visibleItems;
            _scrollBar.Visible = true;
        }
        else
        {
            _scrollBar.Maximum = 0;
            _scrollBar.LargeChange = totalItems;
            _scrollBar.Visible = false;
        }

        // Draw border around entire form (including scrollbar area) with blue highlight color
        using var pen = new Pen(Color.FromArgb(0, 120, 215)); // Windows accent blue, 1px
        e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

        // Draw items
        var scrollbarWidth = _scrollBar.Visible ? _scrollBar.Width : 0;
        var itemAreaWidth = Width - scrollbarWidth - 4;

        for (var i = 0; i < visibleItems && _scrollOffset + i < totalItems; i++)
        {
            var itemIndex = _scrollOffset + i;
            var y = 2 + i * ItemHeight;

            // Draw hover/selection background
            if (itemIndex == _hoveredIndex)
            {
                e.Graphics.FillRectangle(SystemBrushes.Highlight, 2, y, itemAreaWidth, ItemHeight);
            }
            else if (itemIndex == _parent.SelectedIndex)
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(230, 230, 230)), 2, y, itemAreaWidth, ItemHeight);
            }

            // Draw text
            var text = GetItemText(itemIndex);
            var textColour = itemIndex == _hoveredIndex ? SystemColors.HighlightText : SystemColors.WindowText;
            TextRenderer.DrawText(e.Graphics, text, Font, new Rectangle(5, y, itemAreaWidth - 3, ItemHeight), textColour, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }
    }

    private string GetItemText(int index)
    {
        if (index < 0 || index >= _parent.GetItemCount())
        {
            return string.Empty;
        }

        var args = new RetrieveVirtualItemEventArgs(index);
        _parent.OnRetrieveItemInternal(args);

        return args.Item?.Text ?? string.Empty;
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        int itemIndex = _scrollOffset + (e.Y - 2) / ItemHeight;
        int scrollbarWidth = _scrollBar.Visible ? _scrollBar.Width : 0;
        int itemAreaWidth = Width - scrollbarWidth - 4;

        if (itemIndex != _hoveredIndex && itemIndex >= _scrollOffset && itemIndex < _parent.GetItemCount() && e.X < itemAreaWidth + 2)
        {
            _hoveredIndex = itemIndex;
            Invalidate();
        }
    }

    private void OnMouseClick(object? sender, MouseEventArgs e)
    {
        if (_hoveredIndex >= 0 && _hoveredIndex < _parent.GetItemCount())
        {
            _parent.SelectItem(_hoveredIndex);
        }
    }

    private void OnMouseWheel(object? sender, MouseEventArgs e)
    {
        int delta = e.Delta > 0 ? -1 : 1;
        _scrollBar.Value = Math.Max(0, Math.Min(_scrollBar.Maximum, _scrollBar.Value + delta));
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        _parent._isDroppedDown = false;
        _parent.Invalidate();
    }

    public void CloseDropdown()
    {
        Close();
    }
}