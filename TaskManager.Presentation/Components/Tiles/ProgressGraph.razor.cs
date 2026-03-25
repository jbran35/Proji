using BlazorBootstrap;
using Microsoft.AspNetCore.Components;

namespace TaskManager.Presentation.Components.Tiles
{
    /// <summary>
    /// A doughnut char reflecting how close a project is to completion.
    /// </summary>
    public partial class ProgressGraph
    {
        [Parameter] public double PercentComplete { get; set; }
        [Parameter] public bool IsTile { get; set; }

        private DoughnutChart _doughnutChart = default!;
        private DoughnutChartOptions _doughnutChartOptions = default!;
        //private readonly List<double> _projectStats = [2.0]; //Just needs Total and Complete item counts
        private ChartData _chartData = new();

        private bool _isChartInitialized;
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _doughnutChartOptions = new DoughnutChartOptions
                {
                    Responsive = true,
                    Plugins = new DoughnutChartPlugins
                    {
                        Tooltip = new ChartPluginsTooltip { Enabled = false },
                    }
                };

                _chartData = GetChartData();
                await _doughnutChart.InitializeAsync(_chartData, _doughnutChartOptions);

                _isChartInitialized = true;
            }
            else
            {
                await base.OnAfterRenderAsync(firstRender);
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            if (_isChartInitialized)
            {
                var data = GetChartData();
                await _doughnutChart.UpdateAsync(data, _doughnutChartOptions);
            }
        }
        
        private ChartData GetChartData()
        {
            var remaining = 100.00 - PercentComplete;
            var colors = new List<string> { "#28a745", "#dc3545" };
            var labels = new List<string>();
            var data = new List<double?> { PercentComplete, remaining };

            var dataset = new DoughnutChartDataset
            {
                Label = "Progress",
                Data = data,
                BackgroundColor = colors,

            };

            return new ChartData
            {
                Labels = labels,
                Datasets = [dataset]
            };
        }
    }
}
