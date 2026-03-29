using System.Collections.Generic;

namespace ExpenseTracker.API.DTOs.Statistics
{
    public class StatisticsDto
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }

        public List<CategoryStat> CategoryData { get; set; } = new List<CategoryStat>();

        public List<CategoryStat> IncomeCategoryData { get; set; } = new List<CategoryStat>();
    }

    public class CategoryStat
    {
        public string Name { get; set; }
        public decimal Amount { get; set; }
    }
}