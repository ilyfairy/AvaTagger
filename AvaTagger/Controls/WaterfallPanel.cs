using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;

namespace AvaTagger.Controls;

/// <summary>
/// 瀑布流布局面板
/// </summary>
public class WaterfallPanel : Panel
{
    /// <summary>
    /// 列数（当 ItemWidth 未设置时使用）
    /// </summary>
    public static readonly StyledProperty<int> ColumnsProperty =
      AvaloniaProperty.Register<WaterfallPanel, int>(nameof(Columns), 0);

    /// <summary>
    /// 项宽度（优先级高于 Columns）
    /// </summary>
    public static readonly StyledProperty<double> ItemWidthProperty =
        AvaloniaProperty.Register<WaterfallPanel, double>(nameof(ItemWidth), 200.0);

    /// <summary>
    /// 最小列间距（剩余空间会均分到所有间距中）
    /// </summary>
    public static readonly StyledProperty<double> MinColumnSpacingProperty =
        AvaloniaProperty.Register<WaterfallPanel, double>(nameof(MinColumnSpacing), 0.0);

    /// <summary>
    /// 行间距
    /// </summary>
    public static readonly StyledProperty<double> RowSpacingProperty =
 AvaloniaProperty.Register<WaterfallPanel, double>(nameof(RowSpacing), 8.0);

    /// <summary>
    /// 列数（当 ItemWidth 未设置时使用）
    /// </summary>
    public int Columns
    {
        get => GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    /// <summary>
    /// 项宽度（优先级高于 Columns）
    /// </summary>
    public double ItemWidth
    {
        get => GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    /// <summary>
    /// 最小列间距（剩余空间会均分到所有间距中）
    /// </summary>
    public double MinColumnSpacing
    {
        get => GetValue(MinColumnSpacingProperty);
        set => SetValue(MinColumnSpacingProperty, value);
    }

    /// <summary>
    /// 行间距
    /// </summary>
    public double RowSpacing
    {
        get => GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    static WaterfallPanel()
    {
        AffectsMeasure<WaterfallPanel>(ColumnsProperty, ItemWidthProperty, MinColumnSpacingProperty, RowSpacingProperty);
        AffectsArrange<WaterfallPanel>(ColumnsProperty, ItemWidthProperty, MinColumnSpacingProperty, RowSpacingProperty);
    }

    /// <summary>
    /// 计算布局参数（列数、列宽、实际列间距、左边距）
    /// </summary>
    private (int columns, double columnWidth, double actualColumnSpacing, double leftMargin) CalculateLayout(double availableWidth)
    {
        var minColumnSpacing = MinColumnSpacing;
        var itemWidth = ItemWidth;
        var columns = Columns;

        // 如果指定了 ItemWidth，则根据容器宽度自动计算列数
        if (itemWidth > 0)
        {
            // 计算最多能容纳多少列
            // 公式: itemWidth * n + minColumnSpacing * (n - 1) <= availableWidth
            // 解得: n <= (availableWidth + minColumnSpacing) / (itemWidth + minColumnSpacing)
            var maxColumns = Math.Max(1, (int)Math.Floor((availableWidth + minColumnSpacing) / (itemWidth + minColumnSpacing)));

            // 计算剩余空间
            var totalItemWidth = itemWidth * maxColumns;
            var remainingSpace = availableWidth - totalItemWidth;

            // 如果只有一列，不需要间距，居中显示
            if (maxColumns == 1)
            {
                return (1, itemWidth, 0, remainingSpace / 2);
            }

            // 剩余空间平均分配到 (n + 1) 个间距位置（左边、中间n-1个、右边）
            // 这样可以实现居中效果
            var spacingCount = maxColumns + 1;
            var actualSpacing = remainingSpace / spacingCount;

            return (maxColumns, itemWidth, actualSpacing, actualSpacing);
        }
        // 否则使用固定列数
        else if (columns > 0)
        {
            var totalItemWidth = availableWidth;
            var totalSpacing = minColumnSpacing * (columns - 1);
            var columnWidth = (availableWidth - totalSpacing) / columns;

            if (columnWidth < 100)
            {
                columnWidth = 100;
                totalItemWidth = columnWidth * columns;
                totalSpacing = minColumnSpacing * (columns - 1);
            }

            var remainingSpace = availableWidth - totalItemWidth;
            var spacingCount = columns + 1;
            var actualSpacing = remainingSpace / spacingCount;

            return (columns, columnWidth, Math.Max(minColumnSpacing, actualSpacing), actualSpacing);
        }
        // 默认行为
        else
        {
            return (3, (availableWidth - minColumnSpacing * 2) / 3, minColumnSpacing, 0);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var rowSpacing = RowSpacing;
        var availableWidth = double.IsInfinity(availableSize.Width) ? 800 : availableSize.Width;

        var (columns, columnWidth, _, _) = CalculateLayout(availableWidth);

        // 每列的累计高度
        var columnHeights = new double[columns];
        var childIndex = 0;

        foreach (var child in Children)
        {
            if (child.IsVisible)
            {
                // 测量子元素
                child.Measure(new Size(columnWidth, double.PositiveInfinity));

                // 计算当前元素所在的列（按顺序填充，不是找最短列）
                var columnIndex = childIndex % columns;

                // 添加到对应列的高度
                if (columnHeights[columnIndex] > 0)
                {
                    columnHeights[columnIndex] += rowSpacing;
                }
                columnHeights[columnIndex] += child.DesiredSize.Height;

                childIndex++;
            }
        }

        // 返回所需尺寸
        var maxHeight = columnHeights.Length > 0 ? columnHeights.Max() : 0;
        return new Size(availableWidth, maxHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var rowSpacing = RowSpacing;

        var (columns, columnWidth, actualColumnSpacing, leftMargin) = CalculateLayout(finalSize.Width);

        // 每列的累计高度
        var columnHeights = new double[columns];
        var childIndex = 0;

        foreach (var child in Children)
        {
            if (child.IsVisible)
            {
                // 计算当前元素所在的列（按顺序填充，从左到右）
                var columnIndex = childIndex % columns;

                // 如果该列已有元素，则添加行间距
                if (columnHeights[columnIndex] > 0)
                {
                    columnHeights[columnIndex] += rowSpacing;
                }

                // 计算 X 位置（左边距 + 当前列索引 * (列宽 + 列间距)）
                var x = leftMargin + columnIndex * (columnWidth + actualColumnSpacing);

                // 计算 Y 位置
                var y = columnHeights[columnIndex];

                // 排列子元素
                var childRect = new Rect(x, y, columnWidth, child.DesiredSize.Height);
                child.Arrange(childRect);

                // 更新列高度
                columnHeights[columnIndex] += child.DesiredSize.Height;

                childIndex++;
            }
        }

        return finalSize;
    }
}
