using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using True_Mining_Desktop.Janelas;

namespace True_Mining_Desktop.ViewModel
{
    internal class DashboardChart
    {
        public static void UpdateAxes(Dictionary<int, Int64> dados, int zoomInterval)
        {
            PlotModel plotModel = new PlotModel()
            {
                Title = "Mined Points History",
                TitleColor = OxyColor.FromRgb(64, 64, 64),
                TitleFontSize = 15,
                TitleHorizontalAlignment = TitleHorizontalAlignment.CenteredWithinView,
                PlotAreaBorderThickness = new OxyThickness(0, 0, 0, 0),
            };

            Dictionary<int, int> dataToShow = new Dictionary<int, int>();

            Dictionary<string, int> dataToShow_formated = new Dictionary<string, int>();

            List<string> listaLegendaX = new List<string>();

            int botonAxisAngle = 0;

            if (zoomInterval <= TimeSpan.FromDays(1).TotalSeconds) //grafico H1
            {
                zoomInterval = (int)new TimeSpan((int)Math.Floor(TimeSpan.FromSeconds(zoomInterval).TotalHours) - 1, DateTime.UtcNow.Minute, DateTime.UtcNow.Second).TotalSeconds;

                dataToShow = dados.Where((KeyValuePair<int, Int64> value) =>
                value.Key >= ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() - zoomInterval)
                .Select((KeyValuePair<int, Int64> value) => new KeyValuePair<int, int>(value.Key, (int)(value.Value / 840)))
                .OrderBy((KeyValuePair<int, int> value) => value.Key)
                .ToDictionary(x => x.Key, x => x.Value);

                for (int i = 0; zoomInterval / 60 / 60 >= i; i++)
                {
                    DateTime dateTime = DateTime.UtcNow.ToLocalTime().AddSeconds(-zoomInterval).AddHours(i);
                    string labelToAdd = dateTime.Hour.ToString().PadLeft(2, '0') + ":00";
                    listaLegendaX.Add(labelToAdd);
                }

                foreach (KeyValuePair<int, int> pair in dataToShow)
                {
                    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc).AddSeconds(pair.Key).ToLocalTime();
                    string labelToAdd = dateTime.Hour.ToString().PadLeft(2, '0') + ":00";

                    if (dataToShow_formated.ContainsKey(labelToAdd))
                    {
                        dataToShow_formated[labelToAdd] = dataToShow_formated[labelToAdd] + pair.Value;
                    }
                    else
                    {
                        dataToShow_formated.TryAdd(labelToAdd, pair.Value);
                    }
                }
                botonAxisAngle = 90;
            }
            else if (zoomInterval > TimeSpan.FromDays(1).TotalSeconds) //grafico D1
            {
                zoomInterval = (int)new TimeSpan((int)Math.Floor(TimeSpan.FromSeconds(zoomInterval).TotalDays) - 1, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second).TotalSeconds;

                dataToShow = dados.Where((KeyValuePair<int, Int64> value) =>
                value.Key >= ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() - zoomInterval)
                .Select((KeyValuePair<int, Int64> value) => new KeyValuePair<int, int>(value.Key, (int)value.Value / 840))
                .OrderBy((KeyValuePair<int, int> value) => value.Key)
                .ToDictionary(x => x.Key, x => x.Value);

                for (int i = 0; zoomInterval / 60 / 60 / 24 >= i; i++)
                {
                    DateTime dateTime = DateTime.UtcNow.AddSeconds(-zoomInterval).AddDays(i);
                    string labelToAdd = dateTime.ToShortDateString() + " (UTC)";
                    listaLegendaX.Add(labelToAdd);
                }

                foreach (KeyValuePair<int, int> pair in dataToShow)
                {
                    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc).AddSeconds(pair.Key);
                    string labelToAdd = dateTime.ToShortDateString() + " (UTC)";

                    if (dataToShow_formated.ContainsKey(labelToAdd))
                    {
                        dataToShow_formated[labelToAdd] = dataToShow_formated[labelToAdd] + pair.Value;
                    }
                    else
                    {
                        dataToShow_formated.TryAdd(labelToAdd, pair.Value);
                    }

                    if (!listaLegendaX.Contains(labelToAdd))
                    {
                        //   listaLegendaX.Add(labelToAdd);
                    }
                }
                botonAxisAngle = 00;
            }

            plotModel.Axes.Clear();

            CategoryAxis categoryAxis = new CategoryAxis()
            {
                Position = AxisPosition.Bottom,
                AxisTickToLabelDistance = 0,
                MinorGridlineStyle = LineStyle.None,
                MajorGridlineStyle = LineStyle.None,
                MinorStep = 1,
                Angle = botonAxisAngle,
                AxislineStyle = LineStyle.Solid,
                AxislineThickness = 1,
                IsZoomEnabled = false,
                Selectable = false,
                IsPanEnabled = true,
                TickStyle = TickStyle.Crossing,
            };

            foreach (string label in listaLegendaX)
            { categoryAxis.ActualLabels.Add(label); }

            plotModel.Axes.Add(categoryAxis);

            /////////////////////////////////

            Pages.Dashboard.ColumnChartSeries = new OxyPlot.Series.ColumnSeries()
            {
                TrackerFormatString = "{2} points",
                Selectable = false,
                StrokeThickness = 1,
            };

            foreach (KeyValuePair<string, int> keyValuePair in dataToShow_formated)
            {
                Pages.Dashboard.ColumnChartSeries.Items.Add(new ColumnItem(keyValuePair.Value, listaLegendaX.IndexOf(keyValuePair.Key)));
            }
            if (Pages.Dashboard.ColumnChartSeries.Items.Count == 0) Pages.Dashboard.ColumnChartSeries.Items.Add(new ColumnItem(0));

            plotModel.Series.Add(Pages.Dashboard.ColumnChartSeries);

            ////////////////////////////////
            int chart_max_value = dataToShow_formated.Count > 0 ? (int)Math.Ceiling(d: (decimal)dataToShow_formated.Max((KeyValuePair<string, int> value) => value.Value)) : 10;
            int chart_max_range = (int)((int)Math.Ceiling(chart_max_value / Math.Pow(10, chart_max_value.ToString().Length - 1)) * Math.Pow(10, (chart_max_value.ToString().Length - 1)));
            int chart_Yaxis_major_step = (int)Math.Ceiling((decimal)(chart_max_range / 5));

            plotModel.Axes.Add(new OxyPlot.Axes.CategoryAxis()
            {
                MajorStep = chart_Yaxis_major_step < 2 ? 2 : chart_Yaxis_major_step,
                Position = AxisPosition.Left,
                MinorTickSize = 5,
                MajorTickSize = 5,
                MajorGridlineStyle = LineStyle.Dot,
                MinorGridlineStyle = LineStyle.Dot,
                Maximum = chart_max_range < 10 ? 10 : chart_max_range,
                Minimum = 0,
                AbsoluteMinimum = 0,
                AxislineStyle = LineStyle.Solid,
                AxislineThickness = 1,
                //TickStyle = TickStyle.Outside,
                IsTickCentered = true,

                LabelFormatter = (index =>
                {
                    var ratio = (int)Math.Round(10 / 10.0, 0);
                    var label = (int)index;
                    return (ratio <= 1 || label % ratio == 1) ? label.ToString("D") : string.Empty;
                })
            });

            Pages.Dashboard.ChartControler = new PlotController();
            Pages.Dashboard.ChartControler.UnbindAll();
            Pages.Dashboard.ChartControler.BindMouseEnter(PlotCommands.HoverSnapTrack);

            Pages.Dashboard.ChartModel = plotModel;

            Pages.Dashboard.ChartVisibility = Visibility.Visible;
        }
    }
}