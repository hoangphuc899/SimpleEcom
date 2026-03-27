using SimpleEcom.Models;

public interface ICartService
{
    List<CartItem> GetCart();
    void AddToCart(Product product);
    void RemoveFromCart(int productId);
    void UpdateQuantity(int productId, int quantity);
    void ClearCart();
    decimal GetTotal();
}