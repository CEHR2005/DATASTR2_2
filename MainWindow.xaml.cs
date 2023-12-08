using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MapVisualization
{
    public partial class MainWindow : Window
    {
        private List<City> cities;
        private double canvasCenterX;
        private double canvasCenterY;
        private const double centerLatitude = 42.7;
        private const double centerLongitude = 23.32;

        public MainWindow()
        {
            InitializeComponent();
            MapCanvas.Loaded += MapCanvas_Loaded;
        }

        private void MapCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            canvasCenterX = 0;
            canvasCenterY = MapCanvas.ActualHeight / 2;
            double scaleFactor = 120;
            InitializeCities(scaleFactor);
            DrawMap();
        }

        public class DijkstraData
        {
            public City Previous { get; set; }
            public double Distance { get; set; }
        }


        private List<City> FindRoute(City start, City end)
        {
            var notVisited = new HashSet<City>(cities);
            var track = new Dictionary<City, DijkstraData>();
            track[start] = new DijkstraData { Distance = 0, Previous = null };

            while (notVisited.Count != 0)
            {
                City toOpen = null;
                double bestPrice = double.PositiveInfinity;

                foreach (var city in notVisited)
                {
                    if (track.ContainsKey(city) && track[city].Distance < bestPrice)
                    {
                        toOpen = city;
                        bestPrice = track[city].Distance;
                    }
                }

                if (toOpen == null) return new List<City>(); 
                if (toOpen == end) break; 

                foreach (var road in toOpen.Roads)
                {
                    var currentPrice = track[toOpen].Distance + road.Distance;
                    var nextCity = road.Destination;

                    if (!track.ContainsKey(nextCity) || track[nextCity].Distance > currentPrice)
                    {
                        track[nextCity] = new DijkstraData { Distance = currentPrice, Previous = toOpen };
                    }
                }

                notVisited.Remove(toOpen);
            }

            var path = new List<City>();
            var current = end;
            while (current != null)
            {
                path.Add(current);
                current = track[current].Previous;
            }

            path.Reverse();
            return path;
        }

        private void DisplayDistance(List<City> route)
        {
            if (route.Count < 2) return;

            double totalDistance = 0.0;
            for (int i = 0; i < route.Count - 1; i++)
            {
                var fromCity = route[i];
                var toCity = route[i + 1];

                double distance = Math.Sqrt(Math.Pow(fromCity.X - toCity.X, 2) + Math.Pow(fromCity.Y - toCity.Y, 2));
                totalDistance += distance;
            }

            DistanceTextBlock.Text = $"Расстояние: {totalDistance:F2} единиц";
        }

        private void HighlightRoute(List<City> route)
        {
            if (route.Count < 2) return;

            for (int i = 0; i < route.Count - 1; i++)
            {
                var fromCity = route[i];
                var toCity = route[i + 1];

                foreach (var child in MapCanvas.Children)
                {
                    if (child is Line line)
                    {
                        if ((line.X1 == fromCity.X && line.Y1 == fromCity.Y &&
                             line.X2 == toCity.X && line.Y2 == toCity.Y) ||
                            (line.X2 == fromCity.X && line.Y2 == fromCity.Y &&
                             line.X1 == toCity.X && line.Y1 == toCity.Y))
                        {
                            line.Stroke = Brushes.Red;
                        }
                    }
                }
            }
        }

        private void FindRouteButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var child in MapCanvas.Children)
            {
                if (child is Line line)
                {
                    line.Stroke = Brushes.Gray; 
                }
            }
            string startCityName = StartCityComboBox.SelectedItem as string;
            string endCityName = EndCityComboBox.SelectedItem as string;

            if (startCityName == null || endCityName == null) return;

            var startCity = cities.Find(c => c.Name == startCityName);
            var endCity = cities.Find(c => c.Name == endCityName);

            if (startCity == null || endCity == null) return;

            var route = FindRoute(startCity, endCity);

            if (route.Count > 0)
            {
                HighlightRoute(route);
                DisplayDistance(route);
            }
            else
            {
                Console.WriteLine("Route not found");
            }
        }
        private void InitializeCities(double scaleFactor)
        {
            cities = new List<City>();

            var citiesCoordinates = new Dictionary<string, (double, double)>
            {
                { "Varna", (43.2167, 27.9167) },         // Варна
                { "Burgas", (42.5083, 27.4678) },        // Бургас
                { "Dobrich", (43.5667, 27.8333) },       // Добрич
                { "Silistra", (44.1167, 27.2667) },      // Силистра
                { "Razgrad", (43.5333, 26.5167) },       // Разград
                { "Tyrgowishte", (43.2506, 26.5725) },   // Тырговиште
                { "Shumen", (43.2833, 26.9333) },        // Шумен
                { "Veliko Tarnovo", (43.083, 25.65) },   // Велико-Тырново
                { "Sliven", (42.6833, 26.3333) },        // Сливен
                { "Yambol", (42.4837, 26.5107) },        // Ямбол
                { "Kazanlak", (42.617, 25.4) },          // Казанлык
                { "Stara Zagora", (42.433, 25.65) }      // Стара-Загора
            };

            foreach (var cityData in citiesCoordinates)
            {
                var cityName = cityData.Key;
                var (latitude, longitude) = cityData.Value;

                var canvasCoordinates = ConvertToCanvasCoordinates(latitude, longitude, scaleFactor);

                var city = new City(cityName, canvasCoordinates.X, canvasCoordinates.Y);
                cities.Add(city);
            }
            var roadsData = new List<RoadData>
            {
                new RoadData { Origin = "Varna", Destination = "Burgas", Distance = 87, MaxSpeed = 100 },
                new RoadData { Origin = "Burgas", Destination = "Varna", Distance = 87, MaxSpeed = 100 },

                new RoadData { Origin = "Dobrich", Destination = "Varna", Distance = 40, MaxSpeed = 100 },
                new RoadData { Origin = "Varna", Destination = "Dobrich", Distance = 40, MaxSpeed = 100 },

                new RoadData { Origin = "Varna", Destination = "Razgrad", Distance = 130, MaxSpeed = 100 },
                new RoadData { Origin = "Razgrad", Destination = "Varna", Distance = 130, MaxSpeed = 100 },

                new RoadData { Origin = "Razgrad", Destination = "Silistra", Distance = 120, MaxSpeed = 100 },
                new RoadData { Origin = "Silistra", Destination = "Razgrad", Distance = 120, MaxSpeed = 100 },

                new RoadData { Origin = "Dobrich", Destination = "Silistra", Distance = 76, MaxSpeed = 100 },
                new RoadData { Origin = "Silistra", Destination = "Dobrich", Distance = 76, MaxSpeed = 100 },

                new RoadData { Origin = "Dobrich", Destination = "Razgrad", Distance = 168, MaxSpeed = 100 },
                new RoadData { Origin = "Razgrad", Destination = "Dobrich", Distance = 168, MaxSpeed = 100 },

                new RoadData { Origin = "Shumen", Destination = "Razgrad", Distance = 50, MaxSpeed = 100 },
                new RoadData { Origin = "Razgrad", Destination = "Shumen", Distance = 50, MaxSpeed = 100 },

                new RoadData { Origin = "Shumen", Destination = "Tyrgowishte", Distance = 41, MaxSpeed = 100 },
                new RoadData { Origin = "Tyrgowishte", Destination = "Shumen", Distance = 41, MaxSpeed = 100 },

                new RoadData { Origin = "Shumen", Destination = "Burgas", Distance = 96, MaxSpeed = 100 },
                new RoadData { Origin = "Burgas", Destination = "Shumen", Distance = 96, MaxSpeed = 100 },

                new RoadData { Origin = "Tyrgowishte", Destination = "Razgrad", Distance = 32, MaxSpeed = 100 },
                new RoadData { Origin = "Razgrad", Destination = "Tyrgowishte", Distance = 32, MaxSpeed = 100 },

                new RoadData { Origin = "Shumen", Destination = "Varna", Distance = 90, MaxSpeed = 100 },
                new RoadData { Origin = "Varna", Destination = "Shumen", Distance = 90, MaxSpeed = 100 },

                new RoadData { Origin = "Shumen", Destination = "Dobrich", Distance = 95, MaxSpeed = 100 },
                new RoadData { Origin = "Dobrich", Destination = "Shumen", Distance = 95, MaxSpeed = 100 },

                new RoadData { Origin = "Shumen", Destination = "Sliven", Distance = 82, MaxSpeed = 100 },
                new RoadData { Origin = "Sliven", Destination = "Shumen", Distance = 82, MaxSpeed = 100 },

                new RoadData { Origin = "Burgas", Destination = "Sliven", Distance = 115, MaxSpeed = 100 },
                new RoadData { Origin = "Sliven", Destination = "Burgas", Distance = 115, MaxSpeed = 100 },

                new RoadData { Origin = "Yambol", Destination = "Sliven", Distance = 28, MaxSpeed = 100 },
                new RoadData { Origin = "Sliven", Destination = "Yambol", Distance = 28, MaxSpeed = 100 },

                new RoadData { Origin = "Yambol", Destination = "Burgas", Distance = 79, MaxSpeed = 100 },
                new RoadData { Origin = "Burgas", Destination = "Yambol", Distance = 79, MaxSpeed = 100 },

                new RoadData { Origin = "Veliko Tarnovo", Destination = "Razgrad", Distance = 79, MaxSpeed = 100 },
                new RoadData { Origin = "Razgrad", Destination = "Veliko Tarnovo", Distance = 79, MaxSpeed = 100 },

                new RoadData { Origin = "Veliko Tarnovo", Destination = "Sliven", Distance = 79, MaxSpeed = 100 },
                new RoadData { Origin = "Sliven", Destination = "Veliko Tarnovo", Distance = 79, MaxSpeed = 100 },

                new RoadData { Origin = "Tyrgowishte", Destination = "Sliven", Distance = 112, MaxSpeed = 100 },
                new RoadData { Origin = "Sliven", Destination = "Tyrgowishte", Distance = 112, MaxSpeed = 100 },

                new RoadData { Origin = "Veliko Tarnovo", Destination = "Tyrgowishte", Distance = 112, MaxSpeed = 100 },
                new RoadData { Origin = "Tyrgowishte", Destination = "Veliko Tarnovo", Distance = 112, MaxSpeed = 100 },

                new RoadData { Origin = "Stara Zagora", Destination = "Sliven", Distance = 63, MaxSpeed = 100 },
                new RoadData { Origin = "Sliven", Destination = "Stara Zagora", Distance = 63, MaxSpeed = 100 },

                new RoadData { Origin = "Stara Zagora", Destination = "Yambol", Distance = 63, MaxSpeed = 100 },
                new RoadData { Origin = "Yambol", Destination = "Stara Zagora", Distance = 63, MaxSpeed = 100 },


                new RoadData { Origin = "Stara Zagora", Destination = "Kazanlak", Distance = 33, MaxSpeed = 100 },
                new RoadData { Origin = "Kazanlak", Destination = "Stara Zagora", Distance = 33, MaxSpeed = 100 },

                new RoadData { Origin = "Veliko Tarnovo", Destination = "Kazanlak", Distance = 33, MaxSpeed = 100 },
                new RoadData { Origin = "Kazanlak", Destination = "Veliko Tarnovo", Distance = 33, MaxSpeed = 100 },

                new RoadData { Origin = "Sliven", Destination = "Kazanlak", Distance = 84, MaxSpeed = 100 },
                new RoadData { Origin = "Kazanlak", Destination = "Sliven", Distance = 84, MaxSpeed = 100 },

            };

            foreach (var city in cities)
            {
                StartCityComboBox.Items.Add(city.Name);
                EndCityComboBox.Items.Add(city.Name);
                foreach (var roadInfo in roadsData) 
                {
                    if (roadInfo.Origin == city.Name)
                    {
                        var destinationCity = cities.Find(c => c.Name == roadInfo.Destination);
                        if (destinationCity != null)
                        {
                            var road = new Road
                            {
                                Destination = destinationCity,
                                Distance = roadInfo.Distance,
                                MaxSpeed = roadInfo.MaxSpeed
                            };
                            city.Roads.Add(road);
                        }
                    }
                }
            }
        }

        private Point ConvertToCanvasCoordinates(double latitude, double longitude, double scaleFactor)
        {
            double relativeX = longitude - centerLongitude;
            double relativeY = centerLatitude - latitude;

            double canvasX = canvasCenterX + relativeX * scaleFactor;
            double canvasY = canvasCenterY + relativeY * scaleFactor;

            return new Point(canvasX, canvasY);
        }

        private void DrawMap()
        {
            foreach (var city in cities)
            {
                foreach (var road in city.Roads)
                {
                    DrawRoad(city, road.Destination);
                }
                DrawCity(city);
            }
        }
        private void DrawRoad(City from, City to)
        {
            var roadLine = new Line
            {
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y,
                Stroke = Brushes.Gray,
                StrokeThickness = 2
            };
            MapCanvas.Children.Add(roadLine);
        }
        private void DrawCity(City city)
        {
            const double cityDiameter = 10;
            var cityCircle = new Ellipse
            {
                Width = cityDiameter,
                Height = cityDiameter,
                Fill = Brushes.Black
            };

            Canvas.SetLeft(cityCircle, city.X - cityDiameter / 2);
            Canvas.SetTop(cityCircle, city.Y - cityDiameter / 2);
            MapCanvas.Children.Add(cityCircle);

            DrawCityName(city);
        }

        private void DrawCityName(City city)
        {
            const double fontSize = 12;
            const double textOffsetX = 5; // Смещение текста по X
            const double textOffsetY = -5; // Смещение текста по Y
            var typeface = new Typeface(new FontFamily("Arial"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
            var formattedText = new FormattedText(
                city.Name,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black)
            {
                MaxTextWidth = 200
            };

            var textVisual = new DrawingVisual();
            using (var drawingContext = textVisual.RenderOpen())
            {
                // Отрисовка контура текста
                var pen = new Pen(Brushes.White, 2);
                drawingContext.DrawGeometry(null, pen, formattedText.BuildGeometry(new Point(city.X + textOffsetX, city.Y + textOffsetY)));

                // Отрисовка самого текста
                drawingContext.DrawText(formattedText, new Point(city.X + textOffsetX, city.Y + textOffsetY));
            }

            var drawingHost = new VisualHost { Visual = textVisual };
            MapCanvas.Children.Add(drawingHost);
        }

        // Класс VisualHost для отображения DrawingVisual на Canvas
        public class VisualHost : FrameworkElement
        {
            public DrawingVisual Visual { get; set; }

            protected override int VisualChildrenCount => Visual != null ? 1 : 0;

            protected override Visual GetVisualChild(int index)
            {
                if (index != 0) throw new ArgumentOutOfRangeException();
                return Visual;
            }
        }

    }

    public class City
    {
        public string Name { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public List<Road> Roads { get; set; }

        public City(string name, double x, double y)
        {
            Name = name;
            X = x;
            Y = y;
            Roads = new List<Road>();
        }
    }

    public class Road
    {
        public City Destination { get; set; }
        public double Distance { get; set; } // Расстояние в километрах
        public double MaxSpeed { get; set; } // Максимальная скорость в км/ч
    }
}
public class RoadData
{
    public string Origin { get; set; }
    public string Destination { get; set; }
    public double Distance { get; set; } // Расстояние в километрах
    public double MaxSpeed { get; set; } // Максимальная скорость в км/ч
}