namespace SFML
{
    using global::System;
    using Graphics;
    using Window;

    public class ShowPicture
    {
        public void Show()
        {
            Image image = new Image(@"D:\C#_Project\Learning\SFML\Star-Wars-Obi-Wan-Portrait.jpg");
            Texture texture = new Texture(image);
            Sprite sprite = new Sprite(texture);
            
            VideoMode mode = new VideoMode(image.Size.X, image.Size.Y);
            RenderWindow window = new RenderWindow(mode, "SFML.NET");

            window.Closed += (obj, e) => { window.Close(); };
            window.KeyPressed +=
                (sender, e) =>
                {
                    RenderWindow window = (RenderWindow)sender;
                    if (e.Code == Keyboard.Key.Escape)
                    {
                        window.Close();
                    }
                };

            while (window.IsOpen)
            {
                window.DispatchEvents();
                window.Clear();
                window.Draw(sprite);
                window.Display();
            }
        }

        public void Run()
        {
            
            Console.WriteLine("Press ESC key to close window");
            ShowPicture window = new ShowPicture();
            window.Show();
            Console.WriteLine("All done");
        }
    }
}