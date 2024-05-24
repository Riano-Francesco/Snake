using System.Windows;
using System.Windows.Media;

namespace Snake;

public class SnakePart
{
    public UIElement UiElement { get; set; }
    public Point Position { get; set; }
    public bool IsHead { get; set; }
    
    public bool IsTail { get; set; }
    
    public ImageBrush bodydirection { get; set; }
}

public class SnakeHighscore
{
    public string PlayerName { get; set; }

    public int Score { get; set; }
}