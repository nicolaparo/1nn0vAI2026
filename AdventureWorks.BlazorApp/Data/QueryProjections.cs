using System.Linq.Expressions;

namespace AdventureWorks.BlazorApp.Data;

/// <summary>
/// Shared projections from the scaffolded AdventureWorks entities to the UI read models.
/// </summary>
public static class QueryProjections
{
    public static readonly Expression<Func<Customer, CustomerProfile>> ToCustomerProfile = customer =>
        new CustomerProfile
        {
            CustomerID = customer.CustomerID,
            CompanyName = customer.Store != null
                ? customer.Store.Name
                : customer.Person != null ? customer.Person.FirstName + " " + customer.Person.LastName : null,
            FirstName = customer.Person != null ? customer.Person.FirstName : null,
            LastName = customer.Person != null ? customer.Person.LastName : null,
            EmailAddress = customer.Person == null
                ? null
                : customer.Person.EmailAddresses
                    .OrderBy(email => email.EmailAddressID)
                    .Select(email => email.EmailAddress1)
                    .FirstOrDefault(),
            Phone = customer.Person == null
                ? null
                : customer.Person.PersonPhones
                    .OrderBy(phone => phone.PhoneNumberTypeID)
                    .Select(phone => phone.PhoneNumber)
                    .FirstOrDefault(),
        };

    public static readonly Expression<Func<SalesOrderHeader, OrderSummary>> ToOrderSummary = order =>
        new OrderSummary
        {
            SalesOrderID = order.SalesOrderID,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalDue = order.TotalDue,
            CustomerID = order.CustomerID,
            CustomerName = order.Customer.Store != null
                ? order.Customer.Store.Name
                : order.Customer.Person != null
                    ? order.Customer.Person.FirstName + " " + order.Customer.Person.LastName
                    : null,
        };
}
