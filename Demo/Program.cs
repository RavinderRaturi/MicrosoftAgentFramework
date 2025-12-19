using System.Diagnostics;

public class ImageProcessor
{
    private readonly int[] _pixels;
    private readonly int _width, _height;

    public ImageProcessor(int width, int height)
    {
        _width = width;
        _height = height;
        _pixels = new int[width * height];  // Simulate loaded image pixels (ARGB)
    }

    // Sequential version - single thread
    public void ResizeSequential(int newWidth, int newHeight)
    {
        var stopwatch = Stopwatch.StartNew();
        var newPixels = new int[newWidth * newHeight];

        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                // Bilinear interpolation (CPU-intensive pixel math)
                int oldX = (int)((x * _width) / (float)newWidth);
                int oldY = (int)((y * _height) / (float)newHeight);
                newPixels[y * newWidth + x] = InterpolatePixel(oldX, oldY);
            }
        }

        stopwatch.Stop();
        Console.WriteLine($"Sequential: {stopwatch.ElapsedMilliseconds}ms");
    }

    // Multithreaded version using Parallel.For
    public void ResizeParallel(int newWidth, int newHeight)
    {
        var stopwatch = Stopwatch.StartNew();
        var newPixels = new int[newWidth * newHeight];

        // Parallel.For divides work across CPU cores automatically
        Parallel.For(0, newHeight, y =>
        {
            for (int x = 0; x < newWidth; x++)
            {
                int oldX = (int)((x * _width) / (float)newWidth);
                int oldY = (int)((y * _height) / (float)newHeight);
                newPixels[y * newWidth + x] = InterpolatePixel(oldX, oldY);
            }
        });

        stopwatch.Stop();
        Console.WriteLine($"Parallel:   {stopwatch.ElapsedMilliseconds}ms");
    }

    // Simulate expensive pixel interpolation
    private int InterpolatePixel(int x, int y)
    {
        // Simulate heavy computation (10k operations per pixel)
        long sum = 0;
        for (int i = 0; i < 10000; i++)
            sum += x * y * i;
        return (int)(sum % 0xFFFFFF);
    }
}

// Usage
class Program
{
    static void Main()
    {
        var processor = new ImageProcessor(4000, 3000);  // 12MP image

        Console.WriteLine("=== Image Resizing Benchmark ===");
        processor.ResizeSequential(2000, 1500);  // ~6MP output
        processor.ResizeParallel(2000, 1500);    // Same output, multithreaded
    }
}



////var substring = StringHelper.ReturnSubString("abcdef");

//int a = 5;
//int b = 6;



//a = a + b;//11



//b = a - b;//5
//Console.WriteLine(b);
//a = a - b;//6
//Console.WriteLine(a);


//Console.ReadLine();


