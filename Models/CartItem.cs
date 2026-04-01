namespace CafeWebsite.Models
{
    public class CartItem
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        
        // Drink customization fields
        public string? Size { get; set; }
        public int Sweetness { get; set; } = 100;
        public string? IceLevel { get; set; }
        public string? Toppings { get; set; }
        public string? SpecialInstructions { get; set; }
        
        // Whether this item is a customizable drink
        public bool IsCustomizable { get; set; }
        
        // Get the customization summary
        public string GetCustomizationSummary()
        {
            if (!IsCustomizable) return "";
            
            var parts = new List<string>();
            
            if (!string.IsNullOrEmpty(Size) && Size != "Regular")
                parts.Add(Size);
                
            if (Sweetness != 100)
                parts.Add($"{Sweetness}% Sweet");
                
            if (!string.IsNullOrEmpty(IceLevel) && IceLevel != "Regular")
                parts.Add(IceLevel);
                
            if (!string.IsNullOrEmpty(Toppings))
                parts.Add(Toppings);
                
            return parts.Count > 0 ? $"({string.Join(", ", parts)})" : "(Standard)";
        }
        
        // Calculate additional price from customizations
        public decimal GetAdditionalPrice()
        {
            decimal additional = 0;
            
            if (!string.IsNullOrEmpty(Size) && Size == "Large")
                additional += 20;
            else if (!string.IsNullOrEmpty(Size) && Size == "Extra Large")
                additional += 35;
                
            if (!string.IsNullOrEmpty(Toppings))
            {
                var toppingList = Toppings.Split(',', StringSplitOptions.RemoveEmptyEntries);
                additional += toppingList.Length * 15;
            }
            
            return additional;
        }
        
        // Get total price including customizations
        public decimal GetTotalPrice()
        {
            return (Price + GetAdditionalPrice()) * Quantity;
        }
    }
}
