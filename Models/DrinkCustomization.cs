using System.ComponentModel.DataAnnotations;

namespace CafeWebsite.Models
{
    public class DrinkCustomization
    {
        // Size options
        public string Size { get; set; } = "Regular";
        
        // Sweetness level (0-100%)
        public int Sweetness { get; set; } = 100;
        
        // Ice level
        public string IceLevel { get; set; } = "Regular";
        
        // Additional toppings/add-ins
        public string Toppings { get; set; } = "";
        
        // Notes for barista
        public string SpecialInstructions { get; set; } = "";

        public string GetSummary()
        {
            var parts = new List<string>();
            
            if (Size != "Regular")
                parts.Add(Size);
                
            if (Sweetness != 100)
                parts.Add($"{Sweetness}% Sweet");
                
            if (IceLevel != "Regular")
                parts.Add(IceLevel);
                
            if (!string.IsNullOrEmpty(Toppings))
                parts.Add(Toppings);
                
            return parts.Count > 0 ? string.Join(", ", parts) : "Standard";
        }
    }

    public class DrinkOption
    {
        public string Name { get; set; } = "";
        public List<string> Choices { get; set; } = new List<string>();
        public decimal AdditionalPrice { get; set; } = 0;
    }
}