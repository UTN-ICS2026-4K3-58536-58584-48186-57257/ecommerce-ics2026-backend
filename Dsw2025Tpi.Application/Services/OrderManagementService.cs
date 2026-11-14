using Dsw2025Tpi.Application.Constants;
using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using Dsw2025Tpi.Application.Services.Interfaces;
using Dsw2025Tpi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Dsw2025Tpi.Domain.Interfaces;

namespace Dsw2025Tpi.Application.Services;

public class OrderManagementService : IOrderManagementService
{
        private readonly Dsw2025TpiContext _context;
    private readonly IRepository _repository;

    public OrderManagementService(Dsw2025TpiContext context, IRepository repository)
    {
        _context = context;
        _repository = repository;
    }
   
                //Crear orden
    public async Task<OrderModel.OrderResponse> CreateOrderAsync(OrderModel.OrderRequest request)
    {
        if (request.OrderItems == null || !request.OrderItems.Any())
            throw new BusinessException(ErrorMessages.OrderMustContainItems);

        foreach (var item in request.OrderItems)
        {
            if (item.Quantity <= 0)
                throw new BusinessException(ErrorMessages.QuantityMustBeGreaterThanZero);
        }

        // Se le pide al repositorio el cliente
        var customer = await _repository.GetById<Customer>(request.CustomerId);

        if (customer == null)
            throw new KeyNotFoundException(ErrorMessages.CustomerNotFound);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            ShippingAddress = request.ShippingAddress,
            BillingAddress = request.BillingAddress,
            Status = OrderStatus.PENDING,
            OrderItems = new List<OrderItem>(),
            OrderDate = DateTime.UtcNow,
        };

        decimal total = 0;
        var productsToUpdate = new List<Product>();

        foreach (var item in request.OrderItems)
        {
            // Se le pide al repositorio cada producto
            var product = await _repository.First<Product>(p => p.Id == item.ProductId && p.IsActive);

            if (product == null)
                throw new KeyNotFoundException(ErrorMessages.ProductNotFound);

            if (item.Quantity > product.StockQuantity)
                throw new BusinessException(ErrorMessages.InsufficientStock);

            var subtotal = item.Quantity * product.CurrentUnitPrice;

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.CurrentUnitPrice,
                Name = product.Name,
                Description = product.Description,
                SubTotal = subtotal
            };

            order.OrderItems.Add(orderItem);
            product.StockQuantity -= item.Quantity;
            productsToUpdate.Add(product);
            total += subtotal;
        }

        order.TotalAmount = total;

        // Recién cuando todo se validó, se guardan los cambios
        foreach (var product in productsToUpdate.Distinct())
        {
            await _repository.Update(product);
        }

        await _repository.Add(order);

        return new OrderModel.OrderResponse(
            order.Id,
            order.CustomerId,
            customer.Name,
            order.ShippingAddress,
            order.BillingAddress,
            order.TotalAmount,
            order.Status.ToString(),
            order.OrderItems.Select(oi => new OrderModel.OrderItemResponse(
                oi.ProductId,
                oi.Name,
                oi.UnitPrice,
                oi.Quantity,
                oi.SubTotal
            )).ToList()
        );
    }

    //Obtener todas las ordenes
    public async Task<PagedResult<OrderModel.OrderResponse>> GetAllOrdersAsync(
            string? Status,
            Guid? CustomerId,
            string? SearchTerm,
            int PageNumber = 1,
            int PageSize = 10)
    {
        var query = _context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.Customer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Status) && Status.ToLower() != "all")
        {

            if (!Enum.TryParse<OrderStatus>(Status, true, out var statusEnum))
                throw new BusinessException(ErrorMessages.InvalidOrderStatus);

            query = query.Where(o => o.Status == statusEnum);
        }


        if (CustomerId.HasValue)
        {
            var exists = await _context.Customers.AnyAsync(c => c.Id == CustomerId.Value);
            if (!exists)
                throw new BusinessException(ErrorMessages.CustomerNotFound);

            query = query.Where(o => o.CustomerId == CustomerId.Value);
        }


        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            query = query.Where(o => o.Customer.Name.Contains(SearchTerm) || o.Id.ToString().Contains(SearchTerm));
        }


        var totalItems = await query.CountAsync();

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();


        var orderResponses = orders.Select(o => new OrderModel.OrderResponse(
            o.Id,
            o.CustomerId,
            o.Customer.Name,
            o.ShippingAddress,
            o.BillingAddress,
            o.TotalAmount,
            o.Status.ToString(),
            o.OrderItems.Select(oi => new OrderModel.OrderItemResponse(
                oi.ProductId,
                oi.Name,
                oi.UnitPrice,
                oi.Quantity,
                oi.SubTotal
            )).ToList()
        )).ToList();


        return new PagedResult<OrderModel.OrderResponse>
        {
            Total = totalItems,
            Items = orderResponses
        };
    }

    //Obtner una orden por ID
    public async Task<OrderModel.OrderResponse> GetOrderByIdAsync(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            throw new KeyNotFoundException(ErrorMessages.OrderNotFound);
        
        return new OrderModel.OrderResponse(
            order.Id,
            order.CustomerId,
            order.Customer.Name,
            order.ShippingAddress,
            order.BillingAddress,
            order.TotalAmount,
            order.Status.ToString(),
            order.OrderItems.Select(oi => new OrderModel.OrderItemResponse(
                oi.ProductId,
                oi.Name,
                oi.UnitPrice,
                oi.Quantity,
                oi.SubTotal
            )).ToList()
        );
    }

    //Actualizar una orden
    public async Task<OrderModel.OrderResponse> UpdateOrderStatusAsync(Guid id, OrderModel.UpdateStatusRequest request)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            throw new KeyNotFoundException(ErrorMessages.OrderNotFound);

        if (!Enum.TryParse<OrderStatus>(request.NewStatus, ignoreCase: true, out var parsedStatus))
            throw new BusinessException(ErrorMessages.InvalidOrderStatus);

        var allowedTransitions = new Dictionary<string, List<string>>
        {
            [OrderStatus.PENDING.ToString()] = new() { OrderStatus.PROCESSING.ToString(), OrderStatus.CANCELLED.ToString() },
            [OrderStatus.PROCESSING.ToString()] = new() { OrderStatus.SHIPPED.ToString(), OrderStatus.CANCELLED.ToString() },
            [OrderStatus.SHIPPED.ToString()] = new() { OrderStatus.DELIVERED.ToString() },
            [OrderStatus.DELIVERED.ToString()] = new(),
            [OrderStatus.CANCELLED.ToString()] = new()
        };

        var currentStatus = order.Status.ToString();
        if (!allowedTransitions.TryGetValue(currentStatus, out var allowed) || !allowed.Contains(request.NewStatus.ToUpper()))
            throw new BusinessException(string.Format(ErrorMessages.InvalidStatusTransition, currentStatus, request.NewStatus));

        order.Status = parsedStatus;
        await _context.SaveChangesAsync();

        return new OrderModel.OrderResponse(
            order.Id,
            order.CustomerId,
            order.Customer.Name,
            order.ShippingAddress,
            order.BillingAddress,
            order.TotalAmount,
            order.Status.ToString(),
            order.OrderItems.Select(oi => new OrderModel.OrderItemResponse(
                oi.ProductId,
                oi.Name,
                oi.UnitPrice,
                oi.Quantity,
                oi.SubTotal
            )).ToList()
        );
    }
}
