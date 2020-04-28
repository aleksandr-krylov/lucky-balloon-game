using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading; 
using System.Windows.Media.Animation; 


namespace LuckyBalloon
{

    public partial class MainWindow : Window
    {
        private bool IsClicked = false; // indicate if a needle is clicked
        
        private Needle needle = new Needle();

        int score; // counter of popped balloons not having a bomb
        int blastedBalloons; // counter of popped balloons with a bomb inside

        // number of popped balloons with bombs inside when the game is lost
        int maxBlastedBalloons = 10;
        // number of balloons without bombs required to pop in order to win tha game
        int PopBalloonsToWin = 25;

        private MediaPlayer player = new MediaPlayer();

        private DateTime lastAdjustmentTime = DateTime.MinValue;

        private double secondsBetweenAdjustments = 10;
        private double initialSecondsBetweenBalloons = 1.5;
        private double initialSecondsToFloat = 5;
        private double secondsBetweenBalloons;
        private double secondsToFloat;

        private double secondsBetweenBalloonsReduction = 0.2;
        private double secondsToFloatReduction = 0.1;

        // track running storyboards and respective balloons with a dictionary
        private Dictionary<Balloon, Storyboard> storyboards = new Dictionary<Balloon, Storyboard>();

        // timer to generate new balloon
        DispatcherTimer balloonTimer = new DispatcherTimer();

        // timer to monitor current game situation
        DispatcherTimer gameTimer = new DispatcherTimer();


        public MainWindow()
        {
            InitializeComponent();
            gameBoard.Visibility = Visibility.Hidden;

            gameTimer.Tick += gameTimer_Tick;
            balloonTimer.Tick += balloonTimer_Tick;

        }


        private void startGameBtn_Click(object sender, RoutedEventArgs e)
        {
            startGameBtn.IsEnabled = false; // disable Start button

            // set up canvas and game board
            gameTitleBox.Visibility = Visibility.Collapsed;
            gameStatus.Visibility = Visibility.Collapsed;
            gameBoard.Visibility = Visibility.Visible;

            scoreProgressBar.Value = 0;
            bombProgressBar.Value = 0;
            IsClicked = false;

            // reset game indicators
            score = 0;
            blastedBalloons = 0;
            // initialize animation parameters with initial values
            secondsBetweenBalloons = initialSecondsBetweenBalloons;
            secondsToFloat = initialSecondsToFloat;

            // Put a needle on a canvas
            Random random = new Random();
            needle.SetValue(Canvas.LeftProperty,
                (double)(random.Next(0, (int)(playground.ActualWidth - needle.Width))));
            needle.SetValue(Canvas.TopProperty, (double)(random.Next(0, (int)(playground.ActualHeight - needle.Height))));
            playground.Children.Add(needle);

            needle.MouseLeftButtonDown += needle_MouseLeftButtonDown;
            needle.MouseLeftButtonUp += needle_MouseLeftButtonUp;

      
            balloonTimer.Interval = TimeSpan.FromSeconds(secondsBetweenBalloons);
            balloonTimer.Start();
            gameTimer.Start();

        }

        private void StopGame()
        {
            gameTimer.Stop();
            balloonTimer.Stop();

            // remove a needle from a canvas
            playground.Children.Remove(needle);

            // stop all running storyboards and clear the canvas from ballons that are underway
            foreach (KeyValuePair<Balloon, Storyboard> item in storyboards)
            {
                Storyboard storyboard = item.Value;
                Balloon bomb = item.Key;

                storyboard.Stop();
                playground.Children.Remove(bomb);
            }

            storyboards.Clear(); // empty the dictionary

            gameStatus.Visibility = Visibility.Visible;
            // enable the Start button to take a new round
            startGameBtn.IsEnabled = true;
        }


        private void gameTimer_Tick(object sender, EventArgs e)
        {

            // check if adjustments to balloons animation and timer are needed 
            if ((DateTime.Now.Subtract(lastAdjustmentTime).TotalSeconds >
                secondsBetweenAdjustments))
            {
                lastAdjustmentTime = DateTime.Now;

                secondsBetweenBalloons -= secondsBetweenBalloonsReduction;
                secondsToFloat -= secondsToFloatReduction;

                balloonTimer.Interval = TimeSpan.FromSeconds(secondsBetweenBalloons);
            }

            // detect a popped balloon
            foreach (KeyValuePair<Balloon, Storyboard> item in storyboards)
            {
                Balloon balloon = item.Key;
                if (CheckCollision(needle, balloon))
                {
                    // play audio file with popping sound to make things realistic
                    player.Open(new Uri("../../files/pop_sound.mp3", UriKind.RelativeOrAbsolute));
                    player.Play(); 

                    // shut down running storyboard of the popped balloon
                    Storyboard storyboard = item.Value;
                    storyboard.Stop();
                    

                    // discard a balloon from the canvas
                    playground.Children.Remove(balloon);


                    // check if the poppped balloon has a bomb inside
                    if (balloon.HaveBomb)
                    {
                        blastedBalloons++;
                        bombProgressBar.Value += 1;

                        // initiate blast 
                        Blast blast = new Blast();


                        // put a blast icon in place where the balloon
                        // has been popped
                        double currentTop = Canvas.GetTop(balloon);
                        double currentLeft = Canvas.GetLeft(balloon);
                        blast.SetValue(Canvas.TopProperty, currentTop);
                        blast.SetValue(Canvas.LeftProperty, currentLeft);
                        playground.Children.Add(blast);
                        
                        // create explosion animation

                        DoubleAnimation widthAnimation = new DoubleAnimation();
                        widthAnimation.To = 1.5;
                        widthAnimation.Duration = TimeSpan.FromSeconds(0.5);

                       
                        Storyboard storyboard1 = new Storyboard();
                        Storyboard.SetTarget(widthAnimation, blast);
                        Storyboard.SetTargetProperty(widthAnimation, new PropertyPath("RenderTransform.Children[0].ScaleX"));                 
                        storyboard1.Children.Add(widthAnimation);

                        DoubleAnimation heightAnimation = new DoubleAnimation();
                        heightAnimation.To = 1.5;
                        heightAnimation.Duration = TimeSpan.FromSeconds(0.5);
                        Storyboard.SetTarget(heightAnimation, blast);
                        Storyboard.SetTargetProperty(heightAnimation, new PropertyPath("RenderTransform.Children[0].ScaleY"));
                        storyboard1.Children.Add(heightAnimation);
                        
                        storyboard1.Completed += storyboard1_Completed;
                        storyboard1.Begin();
                    }
                    else
                    {
                        score++;
                        scoreProgressBar.Value += 1;

                    }
                }

            }

            // update a game board
            currentScore.Content = score.ToString();
            bombCount.Content = blastedBalloons.ToString();

            // check if the game needs to finished as the number of popped
            // balloons with bombs surpassed the limit
            if (blastedBalloons >= maxBlastedBalloons)
            {
                StopGame();
                gameStatus.Text = "Game is over. You lost!";
            }

            // check if the game is won
            if (score >= PopBalloonsToWin)
            {
                StopGame();
                gameStatus.Text = "Game is over. You won!";
            }
        }


        private void balloonTimer_Tick(object sender, EventArgs e)
        {
            Balloon balloon = new Balloon();
       
            Random random = new Random();
            // decide if the balloon will have a bomb inside
            balloon.HaveBomb = Convert.ToBoolean(random.Next(0, 2));

            // randomly position the balloon on the canvas
            balloon.SetValue(Canvas.LeftProperty,
                (double)(random.Next(0, (int)(playground.ActualWidth - balloon.Width))));
            balloon.SetValue(Canvas.TopProperty, playground.ActualHeight - balloon.Height);
            playground.Children.Add(balloon);

            // create and then run floating animation for the balloon
            Storyboard storyboard = new Storyboard();
            DoubleAnimation floatAnimation = new DoubleAnimation();
            floatAnimation.To = -1000;
            floatAnimation.Duration = TimeSpan.FromSeconds(secondsToFloat);

            Storyboard.SetTarget(floatAnimation, balloon);
            Storyboard.SetTargetProperty(floatAnimation, new PropertyPath("(Canvas.Top)"));
            storyboard.Children.Add(floatAnimation);
            storyboard.Duration = floatAnimation.Duration;
     
            storyboards.Add(balloon, storyboard);
            
            storyboard.Completed += storyboard_Completed;
            storyboard.Begin();
        }

        private void storyboard_Completed(object sender, EventArgs e)
        {
            ClockGroup clockGroup = (ClockGroup)sender;

            DoubleAnimation completedAnimation = (DoubleAnimation)clockGroup.Children[0].Timeline;
            Balloon completedBalloon = (Balloon)Storyboard.GetTarget(completedAnimation);

            // suspend the storyboard of finished animation and
            // remove it from the dictionary
            Storyboard storyboard = (Storyboard)clockGroup.Timeline;
            storyboard.Stop();
            storyboards.Remove(completedBalloon);
            // remove the balloon that flew away
            playground.Children.Remove(completedBalloon);
        }

        public Rect UserControlBounds(FrameworkElement control)
        {
            Point ptTopLeft = new Point(Convert.ToDouble(control.GetValue(Canvas.LeftProperty)), Convert.ToDouble(control.GetValue(Canvas.TopProperty)));
            Point ptBottomRight = new Point(Convert.ToDouble(control.GetValue(Canvas.LeftProperty)) + control.Width, Convert.ToDouble(control.GetValue(Canvas.TopProperty)) + control.Height);
            return new Rect(ptTopLeft, ptBottomRight);
        }



        private bool CheckCollision(FrameworkElement control1, FrameworkElement control2)
        {
            /* check if two objects intersect one another on the canvas*/
            Rect rect1 = UserControlBounds(control1);
            Rect rect2 = UserControlBounds(control2);
            rect1.Intersect(rect2);
            if (rect1 == Rect.Empty) return false;
            else return true;
        }


        private void needle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            IsClicked = false;
        }

        private void needle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            IsClicked = true;
        }


        private void playground_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RectangleGeometry rect = new RectangleGeometry();
            rect.Rect = new Rect(0, 0, playground.ActualWidth, playground.ActualHeight);
            playground.Clip = rect;
        }


        private void playground_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsClicked)
            {
                // if a needle has been clicked, update its position on the canvas
                Point pointerPosition = e.GetPosition(null);
                Point relativePosition = grid.TransformToVisual(playground).Transform(pointerPosition);
                needle.SetValue(Canvas.LeftProperty, relativePosition.X - needle.ActualWidth / 5);
                needle.SetValue(Canvas.TopProperty, relativePosition.Y - needle.ActualHeight / 1.3);
            }
        }

        private void storyboard1_Completed(object sender, EventArgs e)
        {
           
            // when the explosion animation is finished, clear the canvas from the blast icon
            ClockGroup clockGroup = (ClockGroup)sender;
            
            DoubleAnimation completedAnimation = (DoubleAnimation)clockGroup.Children[0].Timeline;
            Blast completedBlast = (Blast)Storyboard.GetTarget(completedAnimation);
            playground.Children.Remove(completedBlast);
        }

    }
}
