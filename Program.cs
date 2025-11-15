using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using WebFashion.Data;
using WebFashion.Services;
using WebFashion.Services.Interface;
using WebFashion.Services.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            // 🔹 Bắt buộc để ASP.NET Core biết claim nào là Role
            RoleClaimType = ClaimTypes.Role,
        };
    });


// 🔹 Add Swagger + JWT Authorization UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebFashion API", Version = "v1" });

    // 🔑 Thêm phần này để Swagger cho nhập Bearer token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token vào đây theo format: Bearer {your token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddHttpContextAccessor();
var app = builder.Build();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    SeedData(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseMiddleware<WebFashion.Middleware.AuthorizationMiddleware>();
app.UseAuthorization();
app.UseCors("AllowFrontend");
app.MapControllers();


app.Run();

void SeedData(ApplicationDbContext context)
{
    if (context.Users.Any())
        return;

    var adminUser = new WebFashion.Models.User
    {
        FirstName = "Admin",
        LastName = "User",
        Email = "admin@webfashion.com",
        PhoneNumber = "1234567890",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
        Role = WebFashion.Models.UserRole.Admin
    };

    var customerUser = new WebFashion.Models.User
    {
        FirstName = "John",
        LastName = "Doe",
        Email = "customer@webfashion.com",
        PhoneNumber = "0987654321",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
        Role = WebFashion.Models.UserRole.User
    };

    var customer2 = new WebFashion.Models.User
    {
        FirstName = "Jane",
        LastName = "Smith",
        Email = "jane@webfashion.com",
        PhoneNumber = "1122334455",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
        Role = WebFashion.Models.UserRole.User
    };

    context.Users.AddRange(adminUser, customerUser, customer2);

    var categories = new[]
    {
        new WebFashion.Models.Category { Name = "Men", Description = "Men's Fashion Collection" },
        new WebFashion.Models.Category { Name = "Women", Description = "Women's Fashion Collection" },
        new WebFashion.Models.Category { Name = "Kids", Description = "Kids' Fashion Collection" },
        new WebFashion.Models.Category { Name = "Accessories", Description = "Fashion Accessories" },
        new WebFashion.Models.Category { Name = "Shoes", Description = "Footwear Collection" }
    };

    context.Categories.AddRange(categories);
    context.SaveChanges();

   var products = new[]
    {
        new WebFashion.Models.Product
        {
            ProductTitle = "Classic White T-Shirt",
            Description = "Comfortable and versatile white t-shirt perfect for everyday wear",
            Price = 699000m, // Converted to VND (29.99 * 26000)
            Quantity = 100,
            BrandName = "Fashion Brand",
            CategoryId = categories[0].Id
        },
        new WebFashion.Models.Product
        {
            ProductTitle = "Blue Denim Jeans",
            Description = "Classic blue denim jeans for everyday wear with perfect fit",
            Price = 2079740m, // Converted to VND (79.99 * 26000)
            Quantity = 50,
            BrandName = "Denim Co",
            CategoryId = categories[0].Id
        },
        new WebFashion.Models.Product
        {
            ProductTitle = "Black Formal Shirt",
            Description = "Elegant black formal shirt for business occasions",
            Price = 1559740m, // Converted to VND (59.99 * 26000)
            Quantity = 35,
            BrandName = "Formal Wear",
            CategoryId = categories[0].Id
        },
        new WebFashion.Models.Product
        {
            ProductTitle = "Summer Dress",
            Description = "Light and breezy summer dress perfect for warm weather",
            Price = 1559740m, // Converted to VND (59.99 * 26000)
            Quantity = 75,
            BrandName = "Fashion Brand",
            CategoryId = categories[1].Id
        },
        new WebFashion.Models.Product
        {
            ProductTitle = "Women's Blazer",
            Description = "Professional women's blazer for office and formal events",
            Price = 2339740m, // Converted to VND (89.99 * 26000)
            Quantity = 40,
            BrandName = "Professional Wear",
            CategoryId = categories[1].Id
        },
        new WebFashion.Models.Product
        {
            ProductTitle = "Floral Skirt",
            Description = "Beautiful floral skirt with comfortable fit",
            Price = 1299740m, // Converted to VND (49.99 * 26000)
            Quantity = 60,
            BrandName = "Fashion Brand",
            CategoryId = categories[1].Id
        },
        new WebFashion.Models.Product
        {
            ProductTitle = "Kids Hoodie",
            Description = "Warm and cozy hoodie for kids with fun designs",
            Price = 1039740m, // Converted to VND (39.99 * 26000)
            Quantity = 60,
            BrandName = "Kids Fashion",
            CategoryId = categories[2].Id
        },
        new WebFashion.Models.Product
        {
            ProductTitle = "Kids T-Shirt Set",
            Description = "Set of 3 colorful t-shirts for kids",
            Price = 909740m, // Converted to VND (34.99 * 26000)
            Quantity = 80,
            BrandName = "Kids Fashion",
            CategoryId = categories[2].Id
        },
        new WebFashion.Models.Product
        {
            ProductTitle = "Leather Belt",
            Description = "Premium leather belt with elegant buckle",
            Price = 1299740m, // Converted to VND (49.99 * 26000)
            Quantity = 40,
            BrandName = "Accessories Plus",
            CategoryId = categories[3].Id
        },
        new WebFashion.Models.Product
        {
            ProductTitle = "Silk Scarf",
            Description = "Luxurious silk scarf in various colors",
            Price = 1039740m, // Converted to VND (39.99 * 26000)
            Quantity = 50,
            BrandName = "Accessories Plus",
            CategoryId = categories[3].Id
        },
        new WebFashion.Models.Product
        {
            ProductTitle = "Leather Handbag",
            Description = "Stylish leather handbag for everyday use",
            Price = 2599740m, // Converted to VND (99.99 * 26000)
            Quantity = 25,
            BrandName = "Luxury Bags",
            CategoryId = categories[3].Id
        },
        new WebFashion.Models.Product
        {
            ProductTitle = "Casual Sneakers",
            Description = "Comfortable casual sneakers for everyday wear",
            Price = 1819740m, // Converted to VND (69.99 * 26000)
            Quantity = 45,
            BrandName = "Shoe Co",
            CategoryId = categories[4].Id
        },
        new WebFashion.Models.Product
        {
            ProductTitle = "Formal Dress Shoes",
            Description = "Elegant formal dress shoes for special occasions",
            Price = 2339740m, // Converted to VND (89.99 * 26000)
            Quantity = 30,
            BrandName = "Formal Shoes",
            CategoryId = categories[4].Id
        },
        new WebFashion.Models.Product
        {
            ProductTitle = "Running Shoes",
            Description = "High-performance running shoes with cushioning",
            Price = 3119740m, // Converted to VND (119.99 * 26000)
            Quantity = 35,
            BrandName = "Sports Wear",
            CategoryId = categories[4].Id
        }
    };

    context.Products.AddRange(products);
    context.SaveChanges();

    var cart1 = new WebFashion.Models.Cart
    {
        UserId = customerUser.Id,
        TotalPrice = 0
    };

    var cart2 = new WebFashion.Models.Cart
    {
        UserId = customer2.Id,
        TotalPrice = 0
    };

    context.Carts.AddRange(cart1, cart2);
    context.SaveChanges();

    var order1 = new WebFashion.Models.Order
    {
        UserId = customerUser.Id,
        TotalAmount = 159.97m,
        Status = WebFashion.Models.OrderStatus.Completed,
        ShippingAddress = System.Text.Json.JsonSerializer.Serialize(new
        {
            FullName = "John Doe",
            Email = "customer@webfashion.com",
            Phone = "0987654321",
            Street = "123 Main St",
            City = "New York",
            State = "NY",
            ZipCode = "10001",
            Country = "USA"
        }),
        PaymentMethod = "card"
    };

    order1.Items.Add(new WebFashion.Models.OrderItem
    {
        ProductId = products[0].Id,
        ProductTitle = products[0].ProductTitle,
        Quantity = 2,
        UnitPrice = 29.99m
    });

    order1.Items.Add(new WebFashion.Models.OrderItem
    {
        ProductId = products[1].Id,
        ProductTitle = products[1].ProductTitle,
        Quantity = 1,
        UnitPrice = 79.99m
    });

    var order2 = new WebFashion.Models.Order
    {
        UserId = customer2.Id,
        TotalAmount = 89.99m,
        Status = WebFashion.Models.OrderStatus.Processing,
        ShippingAddress = System.Text.Json.JsonSerializer.Serialize(new
        {
            FullName = "Jane Smith",
            Email = "jane@webfashion.com",
            Phone = "1122334455",
            Street = "456 Oak Ave",
            City = "Los Angeles",
            State = "CA",
            ZipCode = "90001",
            Country = "USA"
        }),
        PaymentMethod = "paypal"
    };

    order2.Items.Add(new WebFashion.Models.OrderItem
    {
        ProductId = products[3].Id,
        ProductTitle = products[3].ProductTitle,
        Quantity = 1,
        UnitPrice = 59.99m
    });

    order2.Items.Add(new WebFashion.Models.OrderItem
    {
        ProductId = products[8].Id,
        ProductTitle = products[8].ProductTitle,
        Quantity = 1,
        UnitPrice = 49.99m
    });

    context.Orders.AddRange(order1, order2);
    context.SaveChanges();
}
