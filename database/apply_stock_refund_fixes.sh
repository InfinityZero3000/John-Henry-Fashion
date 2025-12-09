#!/bin/bash

# Migration Script for Stock & Refund System Fixes
# Version: 1.0
# Date: December 7, 2025

echo "========================================="
echo "John Henry Website - Stock & Refund Fix"
echo "Migration Script"
echo "========================================="
echo ""

# Check if we're in the correct directory
if [ ! -f "JohnHenryFashionWeb.csproj" ]; then
    echo "❌ Error: Must run from project root directory"
    exit 1
fi

echo "📝 Step 1: Creating database migrations..."
echo ""

# Create migration for ShoppingCartItem.ExpiresAt
echo "Creating migration for cart expiration..."
dotnet ef migrations add AddCartExpirationField \
    --context ApplicationDbContext \
    --output-dir Migrations

if [ $? -ne 0 ]; then
    echo "❌ Failed to create cart expiration migration"
    exit 1
fi

# Create migration for RefundRequest updates
echo ""
echo "Creating migration for refund system..."
dotnet ef migrations add UpdateRefundRequestFields \
    --context ApplicationDbContext \
    --output-dir Migrations

if [ $? -ne 0 ]; then
    echo "❌ Failed to create refund migration"
    exit 1
fi

echo ""
echo "✅ Migrations created successfully!"
echo ""

# Ask user if they want to apply migrations now
read -p "Do you want to apply migrations to database now? (y/n): " apply_now

if [ "$apply_now" = "y" ] || [ "$apply_now" = "Y" ]; then
    echo ""
    echo "📊 Step 2: Applying migrations to database..."
    
    # Update database
    dotnet ef database update --context ApplicationDbContext
    
    if [ $? -eq 0 ]; then
        echo "✅ Database updated successfully!"
    else
        echo "❌ Failed to update database"
        exit 1
    fi
else
    echo ""
    echo "⏭️  Skipping database update. Run manually with:"
    echo "   dotnet ef database update"
fi

echo ""
echo "========================================="
echo "📋 Post-Migration Tasks:"
echo "========================================="
echo ""
echo "1. ✅ Code changes applied:"
echo "   - Fixed double stock deduction in PaymentController"
echo "   - Added cart expiration (7 days)"
echo "   - Smart reserve logic (80% limit)"
echo "   - Complete RefundController with approval flow"
echo "   - Email notifications for refunds"
echo "   - CartCleanupService background job"
echo ""
echo "2. 🧪 Testing required:"
echo "   - Test order flow with COD payment"
echo "   - Test order flow with VNPay payment"
echo "   - Verify stock only deducts once"
echo "   - Test cart expiration (set to 1 minute for testing)"
echo "   - Test refund request → approve → verify stock restored"
echo "   - Test refund request → reject → verify reason shown"
echo ""
echo "3. 📝 Next steps:"
echo "   - Create Views for Refund UI (customer + admin)"
echo "   - Test email templates"
echo "   - Update documentation"
echo "   - Deploy to staging for UAT"
echo ""
echo "========================================="
echo "Migration completed! ✨"
echo "========================================="
