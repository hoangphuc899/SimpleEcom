using Newtonsoft.Json;
using SimpleEcom.Data;
using SimpleEcom.Models;

public class CartService: ICartService
{
    private readonly IHttpContextAccessor _httpcontext;
    private readonly AppDbContext _context;
    private const string CartSessionKey = "cart";
    public CartService(IHttpContextAccessor httpContext, AppDbContext context)
    {
        _httpcontext = httpContext;
        _context = context;
    }
    public List<CartItem> GetCart()
    {
        var sessionData = _httpcontext.HttpContext.Session.GetString(CartSessionKey);
        return (sessionData == null) ? new List<CartItem>() : JsonConvert.DeserializeObject<List<CartItem>>(sessionData);
    }
    public void AddToCart(Product product)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(p => p.ProductId == product.Id);
        if(item == null)
        {
            cart.Add(new CartItem{
               ProductId = product.Id,
               ProductName = product.Name,
               Price = product.Price,
               Quantity = 1
            });
        }else item.Quantity++;
        
        SaveCart(cart);
    }
    private void SaveCart(List<CartItem> cart)
    {
        _httpcontext.HttpContext.Session.SetString(CartSessionKey, JsonConvert.SerializeObject(cart));
    }
    public void ClearCart() => _httpcontext.HttpContext.Session.Remove(CartSessionKey);
    public void RemoveFromCart(int productId)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(p => p.ProductId == productId);
        if(item != null)
        {
            cart.Remove(item);
            SaveCart(cart);
        }
    }
    public void UpdateQuantity(int productId, int quantity)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(p => p.ProductId == productId);
        if(item != null)
            item.Quantity = quantity;
        
        SaveCart(cart);
    }
    public decimal GetTotal()
    {
        var cart = GetCart();
        decimal total = 0;
        foreach(var item in cart)
        {
            decimal itemMoney = item.Price * item.Quantity;
            total += itemMoney;
        }
        return total;
        //return cart.Sum(item => item.Price * item.Quantity);
    }
}