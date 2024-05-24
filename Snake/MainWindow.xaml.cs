using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Snake;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class SnakeWPF : Window
{
    public SnakeWPF()
    {
        InitializeComponent();
        gameTickTimer.Tick += GameTickTimer_Tick;
        LoadHighscoreList();
    }

    private System.Windows.Threading.DispatcherTimer gameTickTimer = new System.Windows.Threading.DispatcherTimer();

    private const int SnakeSquareSize = 20;
    private const int SnakeStartLength = 3;
    private const int SnakeStartSpeed = 400;
    private const int SnakeSpeedThreshold = 10;

    private int snakeLength;
    private int currentScore = 0;

    const int MaxHighscoreListEntryCount = 5;

    private Random rnd = new Random();
    private UIElement snakeFood = null;
    private SolidColorBrush foodBrush = Brushes.Red;

    private SolidColorBrush snakeBodyBrush = Brushes.Green;
    private SolidColorBrush snakeHeadBrush = Brushes.LightGreen;
    private List<SnakePart> snakeParts = new List<SnakePart>();

    public enum SnakeDirection
    {
        Left,
        Right,
        Up,
        Down
    };

    private SnakeDirection snakeDirection = SnakeDirection.Right;

    private void Window_ContentRendered(Object sender, EventArgs e)
    {
        DrawGameArea();
        //StartNewGame();
    }

    private void DrawGameArea()
    {
        bool doneDrawingBackground = false;
        int nextX = 0, nextY = 0;
        int rowCounter = 0;
        bool nextIsOdd = false;

        while (doneDrawingBackground == false)
        {
            Rectangle rect = new Rectangle
            {
                Width = SnakeSquareSize,
                Height = SnakeSquareSize,
                // If in einer zeile geschrieben, geiler scheiß
                Fill = nextIsOdd ? Brushes.Gray : Brushes.Black
            };
            GameArea.Children.Add(rect);
            Canvas.SetTop(rect, nextY);
            Canvas.SetLeft(rect, nextX);

            nextIsOdd = !nextIsOdd;
            nextX += SnakeSquareSize;
            if (nextX >= GameArea.ActualWidth)
            {
                nextX = 0;
                nextY += SnakeSquareSize;
                rowCounter++;
                nextIsOdd = (rowCounter % 2 != 0);
            }

            if (nextY >= GameArea.ActualHeight)
            {
                doneDrawingBackground = true;
            }
        }
    }

    private void DrawSnake()
    {
        foreach (SnakePart snakePart in snakeParts)
        {
            if (snakePart.UiElement == null)
            {
                snakePart.UiElement = new Rectangle()
                {
                    Width = SnakeSquareSize,
                    Height = SnakeSquareSize,
                    Fill = (snakePart.IsHead ? snakeHeadBrush : snakeBodyBrush)
                };
                GameArea.Children.Add(snakePart.UiElement);
                Canvas.SetTop(snakePart.UiElement, snakePart.Position.Y);
                Canvas.SetLeft(snakePart.UiElement, snakePart.Position.X);
            }
        }
    }

    private void StartNewGame()
    {
        bdrWelcomeMessage.Visibility = Visibility.Collapsed;
        bdrHighscoreList.Visibility = Visibility.Collapsed;
        bdrEndOfGame.Visibility = Visibility.Collapsed;
        // Remove potential dead snake parts and leftover food...
        foreach (SnakePart snakeBodyPart in snakeParts)
        {
            if (snakeBodyPart.UiElement != null)
                GameArea.Children.Remove(snakeBodyPart.UiElement);
        }

        snakeParts.Clear();
        if (snakeFood != null)
            GameArea.Children.Remove(snakeFood);

        // Reset stuff
        currentScore = 0;
        snakeLength = SnakeStartLength;
        snakeDirection = SnakeDirection.Right;
        snakeParts.Add(new SnakePart() { Position = new Point(SnakeSquareSize * 5, SnakeSquareSize * 5) });
        gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnakeStartSpeed);

        // Draw the snake again and some new food...
        DrawSnake();
        DrawSnakeFood();

        // Update status
        UpdateGameStatus();

        // Go!        
        gameTickTimer.IsEnabled = true;
    }

    private void DrawSnakeFood()
    {
        Point foodPosition = GetNextFoodPosition();
        snakeFood = new Ellipse()
        {
            Width = SnakeSquareSize,
            Height = SnakeSquareSize,
            Fill = new ImageBrush(new BitmapImage(new Uri("C:\\Users\\csl\\RiderProjects\\Snake\\Snake\\PNG\\Food_new.png", UriKind.Absolute)))
        };
        GameArea.Children.Add(snakeFood);
        Canvas.SetTop(snakeFood, foodPosition.Y);
        Canvas.SetLeft(snakeFood, foodPosition.X);
    }
    

    private Point GetNextFoodPosition()
    {
        int maxX = (int)(GameArea.ActualWidth / SnakeSquareSize);
        int maxY = (int)(GameArea.ActualHeight / SnakeSquareSize);
        int foodX = rnd.Next(0, maxX) * SnakeSquareSize;
        int foodY = rnd.Next(0, maxY) * SnakeSquareSize;

        foreach (SnakePart snakePart in snakeParts)
        {
            if ((snakePart.Position.X == foodX) && (snakePart.Position.Y == foodY))
            {
                return GetNextFoodPosition();
            }

        }

        return new Point(foodX, foodY);
    }

    private void UpdateGameStatus()
    {
        // this.Title = "Snake - Score: " + currentScore + " | Game speed: " + gameTickTimer.Interval.TotalMilliseconds;
        this.tbStatusScore.Text = currentScore.ToString();
        this.tbStatusSpeed.Text = gameTickTimer.Interval.TotalMilliseconds.ToString();
    }

    private void GameTickTimer_Tick(Object sender, EventArgs e)
    {
        MoveSnake();
    }

    private void MoveSnake()
    {
        // Remove the last part of the snake, in preparation of the new part added below  
        while (snakeParts.Count >= snakeLength)
        {
            GameArea.Children.Remove(snakeParts[0].UiElement);
            snakeParts.RemoveAt(0);
        }

        // Next up, we'll add a new element to the snake, which will be the (new) head  
        // Therefore, we mark all existing parts as non-head (body) elements, then  
        // we make sure that they use the body brush
        foreach (SnakePart snakePart in snakeParts)
        {
            (snakePart.UiElement as Rectangle).Fill = snakeBodyBrush;
            snakePart.IsHead = false;
        }

        // Determine in which direction to expand the snake, based on the current direction  
        SnakePart snakeHead = snakeParts[snakeParts.Count - 1];
        double nextX = snakeHead.Position.X;
        double nextY = snakeHead.Position.Y;

        switch (snakeDirection)
        {
            case SnakeDirection.Left:
                nextX -= SnakeSquareSize;
                break;
            case SnakeDirection.Right:
                nextX += SnakeSquareSize;
                break;
            case SnakeDirection.Up:
                nextY -= SnakeSquareSize;
                break;
            case SnakeDirection.Down:
                nextY += SnakeSquareSize;
                break;
        }

        // Now add the new head part to our list of snake parts...
        SnakePart newHead = new SnakePart();
        newHead.Position = new Point(nextX, nextY);
        newHead.IsHead = true;

        snakeParts.Add(newHead);
        // {
        //     Position = new Point(nextX, nextY),
        //     IsHead = true
        // });

        if (DoCollisionCheck())
        {
            if (snakeHead.Position.Y < 20)
            {
                newHead.Position = new Point(nextX, nextY + GameArea.ActualHeight);
            }

            else if (snakeHead.Position.Y >= GameArea.ActualHeight - 20)
            {
                newHead.Position = new Point(nextX, nextY - GameArea.ActualHeight);
            }

            else if (snakeHead.Position.X < 20)
            {
                newHead.Position = new Point(nextX + GameArea.ActualWidth, nextY);
            }

            else if (snakeHead.Position.X >= GameArea.ActualWidth - 20)
            {
                newHead.Position = new Point(nextX - GameArea.ActualWidth, nextY);
            }
        }

        DrawSnake();
    }

    private void Window_KeyUp(Object sender, KeyEventArgs e)
    {
        SnakeDirection originalSnakeDirection = snakeDirection;
        switch (e.Key)
        {
            case Key.Up:
                if (snakeDirection != SnakeDirection.Down)
                {
                    snakeDirection = SnakeDirection.Up;
                }

                break;
            case Key.Down:
                if (snakeDirection != SnakeDirection.Up)
                {
                    snakeDirection = SnakeDirection.Down;
                }

                break;
            case Key.Left:
                if (snakeDirection != SnakeDirection.Right)
                {
                    snakeDirection = SnakeDirection.Left;
                }

                break;
            case Key.Right:
                if (snakeDirection != SnakeDirection.Left)
                {
                    snakeDirection = SnakeDirection.Right;
                }

                break;
            case Key.Space:
                StartNewGame();
                break;
        }

        if (snakeDirection != originalSnakeDirection)
        {
            MoveSnake();
        }
    }

    private bool DoCollisionCheck()
    {
        SnakePart snakeHead = snakeParts[snakeParts.Count - 1];

        if ((snakeHead.Position.X == Canvas.GetLeft(snakeFood)) && (snakeHead.Position.Y == Canvas.GetTop(snakeFood)))
        {
            EatSnakeFood();
            return false;
        }

        if ((snakeHead.Position.Y < 20) || (snakeHead.Position.Y >= GameArea.ActualHeight - 20) ||
            (snakeHead.Position.X < 20) || (snakeHead.Position.X >= GameArea.ActualWidth - 20))
        {
            return true;
        }


        foreach (SnakePart snakeBodyPart in snakeParts.Take(snakeParts.Count - 1))
        {
            if ((snakeHead.Position.X == snakeBodyPart.Position.X) &&
                (snakeHead.Position.Y == snakeBodyPart.Position.Y))
            {
                EndGame();
            }
        }

        return false;
    }

    private void EatSnakeFood()
    {
        snakeLength++;
        currentScore++;
        int timerInterval = Math.Max(SnakeSpeedThreshold,
            (int)gameTickTimer.Interval.TotalMilliseconds - (currentScore * 2));
        gameTickTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);
        GameArea.Children.Remove(snakeFood);
        DrawSnakeFood();
        UpdateGameStatus();
    }

    private void EndGame()
    {
        bool isNewHighscore = false;
        if (currentScore > 0)
        {
            int lowestHighscore = (this.HighscoreList.Count > 0 ? this.HighscoreList.Min(x => x.Score) : 0);
            if ((currentScore > lowestHighscore) || (this.HighscoreList.Count < MaxHighscoreListEntryCount))
            {
                bdrNewHighscore.Visibility = Visibility.Visible;
                txtPlayerName.Focus();
                isNewHighscore = true;
            }
        }

        if (!isNewHighscore)
        {
            tbFinalScore.Text = currentScore.ToString();
            bdrEndOfGame.Visibility = Visibility.Visible;
        }

        gameTickTimer.IsEnabled = false;
        MessageBox.Show("Ooops, you died! \n\n To start a new game, just press the Spacebar!", "Snake");
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        this.DragMove();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void BtnShowHighscoreList_Click(object sender, RoutedEventArgs e)
    {
        bdrWelcomeMessage.Visibility = Visibility.Collapsed;
        bdrHighscoreList.Visibility = Visibility.Visible;
    }

    private void LoadHighscoreList()
    {
        if (File.Exists("snake_highscorelist.xml"))
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<SnakeHighscore>));
            using (Stream reader = new FileStream("snake_highscorelist.xml", FileMode.Open))
            {
                List<SnakeHighscore> tempList = (List<SnakeHighscore>)serializer.Deserialize(reader);
                this.HighscoreList.Clear();
                foreach (var item in tempList.OrderByDescending(x => x.Score))
                    this.HighscoreList.Add(item);
            }
        }
    }

    private void SaveHighscoreList()
    {
        XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<SnakeHighscore>));
        using (Stream writer = new FileStream("snake_highscorelist.xml", FileMode.Create))
        {
            serializer.Serialize(writer, this.HighscoreList);
        }
    }

    private void BtnAddToHighscoreList_Click(object sender, RoutedEventArgs e)
    {
        int newIndex = 0;
        // Where should the new entry be inserted?
        if ((this.HighscoreList.Count > 0) && (currentScore < this.HighscoreList.Max(x => x.Score)))
        {
            SnakeHighscore justAbove =
                this.HighscoreList.OrderByDescending(x => x.Score).First(x => x.Score >= currentScore);
            if (justAbove != null)
                newIndex = this.HighscoreList.IndexOf(justAbove) + 1;
        }

        // Create & insert the new entry
        this.HighscoreList.Insert(newIndex, new SnakeHighscore()
        {
            PlayerName = txtPlayerName.Text,
            Score = currentScore
        });
        // Make sure that the amount of entries does not exceed the maximum
        while (this.HighscoreList.Count > MaxHighscoreListEntryCount)
            this.HighscoreList.RemoveAt(MaxHighscoreListEntryCount);

        SaveHighscoreList();

        bdrNewHighscore.Visibility = Visibility.Collapsed;
        bdrHighscoreList.Visibility = Visibility.Visible;
    }

    public ObservableCollection<SnakeHighscore> HighscoreList { get; set; } =
        new ObservableCollection<SnakeHighscore>();
}