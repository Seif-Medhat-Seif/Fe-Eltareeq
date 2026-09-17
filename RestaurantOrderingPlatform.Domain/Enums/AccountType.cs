namespace RestaurantOrderingPlatform.Domain.Enums;

// A user picks exactly one at registration (BR-1: one role for the account's lifetime).
public enum AccountType
{
    Customer,
    RestaurantOwner,
    Driver
}
