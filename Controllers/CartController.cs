using CafeWebsite.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CafeWebsite.Controllers
{
    public class CartController : Controller
    {
        // Get cart from session or create new
        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson))
                return new List<CartItem>();
            return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        // Save cart to session
        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            decimal total = cart.Sum(item => item.GetTotalPrice());
            ViewBag.Total = total;
            return View(cart);
        }

        [HttpPost]
        public IActionResult Add(int id, string name, decimal price, int quantity = 1)
        {
            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(c => c.MenuItemId == id);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    MenuItemId = id,
                    Name = name,
                    Price = price,
                    Quantity = quantity
                });
            }

            SaveCart(cart);
            TempData["Success"] = $"{name} added to cart!";
            return RedirectToAction("Index", "Menu");
        }

        [HttpPost]
        public IActionResult AddWithCustomization(
            int id, 
            string name, 
            decimal price, 
            int quantity = 1,
            string? size = null,
            int sweetness = 100,
            string? iceLevel = null,
            string? toppings = null,
            string? specialInstructions = null,
            bool isCustomizable = false)
        {
            var cart = GetCart();
            
            // Create unique key for cart item including customizations
            var cartKey = $"{id}_{size}_{sweetness}_{iceLevel}_{toppings}";
            var existingItem = cart.FirstOrDefault(c => 
                c.MenuItemId == id && 
                c.Size == size && 
                c.Sweetness == sweetness && 
                c.IceLevel == iceLevel && 
                c.Toppings == toppings);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                var newItem = new CartItem
                {
                    MenuItemId = id,
                    Name = name,
                    Price = price,
                    Quantity = quantity,
                    Size = size ?? "Regular",
                    Sweetness = sweetness,
                    IceLevel = iceLevel ?? "Regular",
                    Toppings = toppings ?? "",
                    SpecialInstructions = specialInstructions ?? "",
                    IsCustomizable = isCustomizable
                };
                cart.Add(newItem);
            }

            SaveCart(cart);
            TempData["Success"] = $"{name} added to cart!";
            return RedirectToAction("Index", "Menu");
        }

        [HttpPost]
        public IActionResult Update(int id, int quantity, string? size = null, int sweetness = 100, string? iceLevel = null, string? toppings = null)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => 
                c.MenuItemId == id && 
                c.Size == size && 
                c.Sweetness == sweetness && 
                c.IceLevel == iceLevel && 
                c.Toppings == toppings);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
            }

            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Remove(int id, string? size = null, int sweetness = 100, string? iceLevel = null, string? toppings = null)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => 
                c.MenuItemId == id && 
                c.Size == size && 
                c.Sweetness == sweetness && 
                c.IceLevel == iceLevel && 
                c.Toppings == toppings);

            if (item != null)
            {
                cart.Remove(item);
            }

            SaveCart(cart);
            TempData["Success"] = "Item removed from cart";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Clear()
        {
            SaveCart(new List<CartItem>());
            TempData["Success"] = "Cart cleared";
            return RedirectToAction(nameof(Index));
        }

        public int GetCartCount()
        {
            return GetCart().Sum(c => c.Quantity);
        }
        
        // Check if a category is customizable
        public static bool IsDrinkCategory(string category)
        {
            var drinkCategories = new[] { "coffee", "non-coffee", "specials", "latte", "tea", "frappe", "milk tea" };
            return drinkCategories.Contains(category.ToLower());
        }
    }
}
