using System;
using System.Data;
using System.Linq;
using System.Windows;

namespace Safonova
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeGrids();
        }

        private void InitializeGrids()
        {
            // Инициализация матрицы затрат
            DataTable costMatrixTable = new DataTable();
            for (int i = 0; i < 3; i++) // По умолчанию 3 столбца
                costMatrixTable.Columns.Add($"Пункт {i + 1}");
            for (int i = 0; i < 3; i++) // По умолчанию 3 строки
                costMatrixTable.Rows.Add(costMatrixTable.NewRow());
            costMatrixGrid.ItemsSource = costMatrixTable.DefaultView;

            // Инициализация запасов
            DataTable supplyTable = new DataTable();
            supplyTable.Columns.Add("Запасы");
            for (int i = 0; i < 3; i++) // По умолчанию 3 строки
                supplyTable.Rows.Add(supplyTable.NewRow());
            supplyGrid.ItemsSource = supplyTable.DefaultView;

            // Инициализация потребностей
            DataTable demandTable = new DataTable();
            demandTable.Columns.Add("Потребности");
            for (int i = 0; i < 3; i++) // По умолчанию 3 строки
                demandTable.Rows.Add(demandTable.NewRow());
            demandGrid.ItemsSource = demandTable.DefaultView;
        }

        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            var costMatrixTable = ((DataView)costMatrixGrid.ItemsSource).ToTable();
            costMatrixTable.Rows.Add(costMatrixTable.NewRow());
            costMatrixGrid.ItemsSource = costMatrixTable.DefaultView;

            var supplyTable = ((DataView)supplyGrid.ItemsSource).ToTable();
            supplyTable.Rows.Add(supplyTable.NewRow());
            supplyGrid.ItemsSource = supplyTable.DefaultView;
        }

        private void AddColumn_Click(object sender, RoutedEventArgs e)
        {
            var costMatrixTable = ((DataView)costMatrixGrid.ItemsSource).ToTable();
            string newColumnName = $"Пункт {costMatrixTable.Columns.Count + 1}";
            costMatrixTable.Columns.Add(newColumnName);
            costMatrixGrid.ItemsSource = costMatrixTable.DefaultView;

            var demandTable = ((DataView)demandGrid.ItemsSource).ToTable();
            demandTable.Rows.Add(demandTable.NewRow());
            demandGrid.ItemsSource = demandTable.DefaultView;
        }

        private int[,] ReadCostMatrix()
        {
            DataTable table = ((DataView)costMatrixGrid.ItemsSource).ToTable();
            int rows = table.Rows.Count;
            int cols = table.Columns.Count;
            int[,] matrix = new int[rows, cols];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    matrix[i, j] = int.TryParse(table.Rows[i][j]?.ToString(), out var value) ? value : 0;

            return matrix;
        }

        private int[] ReadSupply()
        {
            DataTable table = ((DataView)supplyGrid.ItemsSource).ToTable();
            return table.AsEnumerable().Select(row => int.TryParse(row[0]?.ToString(), out var value) ? value : 0).ToArray();
        }

        private int[] ReadDemand()
        {
            DataTable table = ((DataView)demandGrid.ItemsSource).ToTable();
            return table.AsEnumerable().Select(row => int.TryParse(row[0]?.ToString(), out var value) ? value : 0).ToArray();
        }

        private void NorthWestButton_Click(object sender, RoutedEventArgs e)
        {
            SolveTransportationProblem(NorthWestMethod);
        }

        private void MinElementButton_Click(object sender, RoutedEventArgs e)
        {
            SolveTransportationProblem(MinElementMethod);
        }

        private void SolveTransportationProblem(Func<int[,], int[], int[], (int[,], double)> method)
        {
            try
            {
                int[,] costMatrix = ReadCostMatrix();
                int[] supply = ReadSupply();
                int[] demand = ReadDemand();

                int totalSupply = supply.Sum();
                int totalDemand = demand.Sum();

                // Проверка и добавление фиктивных переменных
                if (totalSupply < totalDemand)
                {
                    // Добавляем фиктивный источник
                    int[,] newCostMatrix = new int[costMatrix.GetLength(0) + 1, costMatrix.GetLength(1)];
                    Array.Copy(costMatrix, newCostMatrix, costMatrix.Length);
                    for (int j = 0; j < costMatrix.GetLength(1); j++)
                    {
                        newCostMatrix[costMatrix.GetLength(0), j] = 0; // Затраты фиктивного источника равны 0
                    }

                    costMatrix = newCostMatrix;

                    int[] newSupply = new int[supply.Length + 1];
                    Array.Copy(supply, newSupply, supply.Length);
                    newSupply[supply.Length] = totalDemand - totalSupply; // Добавляем недостающий запас

                    supply = newSupply;

                    MessageBox.Show("Общий запас меньше общей потребности. Добавлен фиктивный источник.");
                }
                else if (totalSupply > totalDemand)
                {
                    // Добавляем фиктивный пункт назначения
                    int[,] newCostMatrix = new int[costMatrix.GetLength(0), costMatrix.GetLength(1) + 1];
                    for (int i = 0; i < costMatrix.GetLength(0); i++)
                    {
                        for (int j = 0; j < costMatrix.GetLength(1); j++)
                        {
                            newCostMatrix[i, j] = costMatrix[i, j];
                        }
                        newCostMatrix[i, costMatrix.GetLength(1)] = 0; // Затраты фиктивного пункта равны 0
                    }

                    costMatrix = newCostMatrix;

                    int[] newDemand = new int[demand.Length + 1];
                    Array.Copy(demand, newDemand, demand.Length);
                    newDemand[demand.Length] = totalSupply - totalDemand; // Добавляем недостающий спрос

                    demand = newDemand;

                    MessageBox.Show("Общая потребность меньше общего запаса. Добавлен фиктивный пункт назначения.");
                }

                // Решение транспортной задачи
                var (distribution, cost) = method(costMatrix, supply, demand);
                totalCostTextBlock.Text = $"Общая стоимость F(x): {cost}";

                // Формирование таблицы результата
                DataTable resultTable = new DataTable();
                for (int i = 0; i < distribution.GetLength(1); i++)
                    resultTable.Columns.Add($"Пункт {i + 1}");
                for (int i = 0; i < distribution.GetLength(0); i++)
                {
                    DataRow row = resultTable.NewRow();
                    for (int j = 0; j < distribution.GetLength(1); j++)
                        row[j] = distribution[i, j];
                    resultTable.Rows.Add(row);
                }
                resultDataGrid.ItemsSource = resultTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }


        private (int[,], double) NorthWestMethod(int[,] costMatrix, int[] supply, int[] demand)
        {
            return SolveByMethod(costMatrix, supply, demand, isNorthWest: true);
        }

        private (int[,], double) MinElementMethod(int[,] costMatrix, int[] supply, int[] demand)
        {
            return SolveByMethod(costMatrix, supply, demand, isNorthWest: false);
        }

        private (int[,], double) SolveByMethod(int[,] costMatrix, int[] supply, int[] demand, bool isNorthWest)
        {
            double totalCost = 0;
            int[,] distribution = new int[supply.Length, demand.Length];

            while (supply.Any(s => s > 0) && demand.Any(d => d > 0))
            {
                int row = -1, col = -1;

                if (isNorthWest)
                {
                    for (int i = 0; i < supply.Length; i++)
                    {
                        for (int j = 0; j < demand.Length; j++)
                        {
                            if (supply[i] > 0 && demand[j] > 0)
                            {
                                row = i;
                                col = j;
                                break;
                            }
                        }
                        if (row != -1) break;
                    }
                }
                else
                {
                    int minCost = int.MaxValue;
                    for (int i = 0; i < supply.Length; i++)
                    {
                        for (int j = 0; j < demand.Length; j++)
                        {
                            if (supply[i] > 0 && demand[j] > 0 && costMatrix[i, j] < minCost)
                            {
                                minCost = costMatrix[i, j];
                                row = i;
                                col = j;
                            }
                        }
                    }
                }

                int quantity = Math.Min(supply[row], demand[col]);
                distribution[row, col] = quantity;
                supply[row] -= quantity;
                demand[col] -= quantity;
                totalCost += quantity * costMatrix[row, col];
            }

            return (distribution, totalCost);
        }
    }
}
