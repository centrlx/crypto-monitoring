// Кароче мы решили использовать библеотеки поставленные через менеджер пакетов NuGet такие как:
// Sqlite и Newtonsoft

using System.Text.Json;
using System.Data.SQLite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Practicheskaya.Form1;

namespace Practicheskaya
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // Start of Global Classes
        public class Coin
        {
            [DisplayName("Название")]
            public string Name { get; set; }
            public decimal Price_usd { get; set; }
            [DisplayName("Цена (USD)")]
            public decimal Price { get; set; }
        }

        public class Currency
        {
            public string Name { get; set; }
            public decimal currencyValueFromUSD { get; set; }
        }

        public class CoinHistory
        {
            public int Id { get; set; }
            [DisplayName("Название")]
            public string Name { get; set; }
            [DisplayName("Цена")]
            public decimal Price { get; set; }
            [DisplayName("Валюта")]
            public string Currency { get; set; }
            [DisplayName("Создано в")]
            public string CreatedAt { get; set; }
        }
        // End of Global Classes

        // Start of Global Objects
        List<Currency> listOfCurrencies = new List<Currency>()
        {
            new Currency { Name = "USD", currencyValueFromUSD = 1m },
            new Currency { Name = "EUR", currencyValueFromUSD = 0.866132m },
            new Currency { Name = "RUB", currencyValueFromUSD = 81.668706m },
            new Currency { Name = "KZT", currencyValueFromUSD = 482.110126m }
        };

        // End of Global Objects

        // Start of Global Vars
        const string HTTPSCRYPTOREQUESTLINK = "https://api.coingecko.com/" +
            "api/v3/simple/" +
            "price?vs_currencies=usd&" +
            "ids=bitcoin,ethereum,monero&" +
            "x_cg_demo_api_key=";
        const string CRYPTOAPIKEY = "CG-KKHyJ58pym5aisq39naYiQCa";

        private static readonly HttpClient client = new HttpClient();
        // End of Global Vars

        // Start of Global Functions
        private async Task asyncProgressBarWork()
        {
            progressBar1.Value = 0;


            while (progressBar1.Value < 100)
            {
                progressBar1.Value++;
                await Task.Delay(1);
            }
        }
        // I am not proud of this code
        private async Task<List<Coin>> parseRequestData()
        {

            string responsedJson = await client.GetStringAsync(HTTPSCRYPTOREQUESTLINK + CRYPTOAPIKEY);

            JObject parsedJson = JObject.Parse(responsedJson);

            if (parsedJson["status"] != null)
            {
                int errorCode = parsedJson["status"]["error_code"].Value<int>();

                if (errorCode == 429)
                {
                    MessageBox.Show("Лимит запросов превышен");
                    return new List<Coin>();
                }
            }

            List<Coin> outputCoinList = new List<Coin>();

            foreach (KeyValuePair<string, JToken> coin in parsedJson)
            {
                if (coin.Key == "status") continue;

                outputCoinList.Add(new Coin
                {
                    Name = coin.Key,
                    Price_usd = coin.Value["usd"]?.Value<decimal>() ?? 0,
                    Price = coin.Value["usd"]?.Value<decimal>() ?? 0
                });
            }
            return outputCoinList;
        }

        private async void loadData()
        {
            try
            {
                progressBar1.Visible = true;

                await asyncProgressBarWork();

                List<Coin> data = await parseRequestData();

                dataGridView1.DataSource = data;

                dataGridView1.Columns["Price_usd"].Visible = false;


            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла Ошибка: " + ex.Message);
            }
            finally
            {
                progressBar1.Visible = false;
            }
        }

        private List<CoinHistory> LoadFromDB()
        {
            var list = new List<CoinHistory>();

            string path = Path.Combine(Application.StartupPath, "Data", "Coins.db");

            using (var connection = new SQLiteConnection($"Data Source={path}"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT Id, Name, Price, Currency, CreatedAt FROM Coins";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new CoinHistory
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Price = reader.GetDecimal(2),
                            Currency = reader.GetString(3),
                            CreatedAt = reader.GetString(4)
                        });
                    }
                }
            }
            return list;
        }

        private void LoadHistoryToGrid()
        {
            dataGridView2.DataSource = LoadFromDB();
        }

        private void buttonExportJson_Click(object sender, EventArgs e)
        {
            try
            {
                List<CoinHistory> list = LoadFromDB();

                string path = Path.Combine(Application.StartupPath, "coins.json");

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = System.Text.Json.JsonSerializer.Serialize(list, options);

                File.WriteAllText(path, json);

                MessageBox.Show("Экспортировано: " + path);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }
        // End of Global Functions

        // Примечание: самый начальный метод вызывающийся при создании самой формы
        private async void Form1_Load(object sender, EventArgs e)
        {
            loadData();
            LoadHistoryToGrid();
        }
        // Конец Примечания

        // Start of Windowsform Events
        private void upadateDataButton_Click(object sender, EventArgs e)
        {
            loadData();
        }

        private void deleteFromDB_Click(object sender, EventArgs e)
        {
            if (dataGridView2.CurrentRow == null)
            {
                MessageBox.Show("Выбери строку");
                return;
            }

            var item = (CoinHistory)dataGridView2.CurrentRow.DataBoundItem;

            string path = Path.Combine(Application.StartupPath, "Data", "Coins.db");

            using (var connection = new SQLiteConnection($"Data Source={path}"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Coins WHERE Id = @id";
                command.Parameters.AddWithValue("@id", item.Id);

                command.ExecuteNonQuery();
            }

            // обновляем грид
            LoadHistoryToGrid();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<Coin> list = (List<Coin>)dataGridView1.DataSource;

            int currencyIndex = comboBox1.SelectedIndex;

            if (currencyIndex < 0) return;

            decimal rate = listOfCurrencies[currencyIndex].currencyValueFromUSD;

            for (int i = 0; i < list.Count; i++)
            {
                list[i].Price = list[i].Price_usd * rate;
            }

            dataGridView1.Columns["Price"].HeaderText = $"Цена (" +
                $"{listOfCurrencies[currencyIndex].Name})";

            dataGridView1.Refresh();
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null)
            {
                MessageBox.Show("Выбери строку");
                return;
            }

            var coin = (Coin)dataGridView1.CurrentRow.DataBoundItem;
            

            int currencyIndex = comboBox1.SelectedIndex;
            if (currencyIndex < 0) return;

            var currency = listOfCurrencies[currencyIndex];

            string folder = Path.Combine(Application.StartupPath, "Data");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string path = Path.Combine(folder, "Coins.db");

            using (var connection = new SQLiteConnection($"Data Source={path}"))
            {
                connection.Open();

                // создаём таблицу если нет
                var create = connection.CreateCommand();
                create.CommandText =
                @"
            CREATE TABLE IF NOT EXISTS Coins (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT,
                Price REAL,
                Currency TEXT,
                CreatedAt TEXT
            );
        ";
                create.ExecuteNonQuery();

                MessageBox.Show("Сохранено по пути:" + path);

                // вставка
                var command = connection.CreateCommand();
                command.CommandText =
                @"
            INSERT INTO Coins (Name, Price, Currency, CreatedAt)
            VALUES ($name, $price, $currency, $time);
        ";

                command.Parameters.AddWithValue("$name", coin.Name);
                command.Parameters.AddWithValue("$price", coin.Price);
                command.Parameters.AddWithValue("$currency", currency.Name);
                command.Parameters.AddWithValue("$time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                command.ExecuteNonQuery();
            }
        }
        private void updateDataBaseList_Click(object sender, EventArgs e)
        {
            LoadHistoryToGrid();
        }

        private void buttonDelete_Click(object sender, EventArgs e)
        {
            if (dataGridView2.CurrentRow == null)
            {
                MessageBox.Show("Выбери строку");
                return;
            }

            var item = (CoinHistory)dataGridView2.CurrentRow.DataBoundItem;

            string path = Path.Combine(Application.StartupPath, "Data", "Coins.db");

            using (var connection = new SQLiteConnection($"Data Source={path}"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Coins WHERE Id = @id";
                command.Parameters.AddWithValue("@id", item.Id);

                command.ExecuteNonQuery();
            }

            // обновляем грид
            LoadHistoryToGrid();
        }
        // End of Windowsform Events
    }
}
