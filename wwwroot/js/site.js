// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Footer scroll behavior
$(window).scroll(function() {
    var scrollTop = $(this).scrollTop();
    var windowHeight = $(this).height();
    var documentHeight = $(document).height();

    if (scrollTop > 100) {
        $('.footer').addClass('show');
    } else {
        $('.footer').removeClass('show');
    }

    if (scrollTop + windowHeight > documentHeight - 200) {
        $('.footer').addClass('fade');
    } else {
        $('.footer').removeClass('fade');
    }
});

// Open customization modal for drinks
function openCustomizeModal(itemId, itemName, itemPrice, isCustomizable) {
    if (!isCustomizable) {
        // If not customizable, add directly to cart
        addToCartDirect(itemId, itemName, itemPrice);
        return;
    }
    
    // Set the item details in the modal (using the IDs from Details.cshtml)
    document.getElementById('detailItemId').value = itemId;
    document.getElementById('detailItemName').value = itemName;
    document.getElementById('detailItemPrice').value = itemPrice;
    
    // Reset customization options to defaults
    var sizeRegular = document.querySelector('input[name="size"][value="Regular"]');
    if (sizeRegular) sizeRegular.checked = true;
    
    // Reset sweetness slider
    var sweetnessSlider = document.getElementById('detailSweetness');
    if (sweetnessSlider) {
        sweetnessSlider.value = 100;
        var sweetnessText = document.getElementById('sweetnessText');
        if (sweetnessText) sweetnessText.textContent = '100%';
    }
    
    var iceRegular = document.querySelector('input[name="iceLevel"][value="Regular"]');
    if (iceRegular) iceRegular.checked = true;
    
    // Clear toppings
    var toppingCheckboxes = document.querySelectorAll('#customizationFormDetail input[name="toppings"]');
    toppingCheckboxes.forEach(function(cb) {
        cb.checked = false;
    });
    
    // Clear special instructions
    var specialInput = document.getElementById('detailInstructions');
    if (specialInput) specialInput.value = '';
    
    // Reset quantity
    var qtyInput = document.getElementById('detailQty');
    if (qtyInput) qtyInput.value = 1;
    
    // Update total price display
    updateDetailTotal();
    
    // Show the modal using Bootstrap
    $('#customizeModal').modal('show');
}

// Update total price in detail modal
function updateDetailTotal() {
    var basePrice = parseFloat(document.getElementById('detailItemPrice').value) || 0;
    var sizeInput = document.querySelector('input[name="size"]:checked');
    var size = sizeInput ? sizeInput.value : 'Regular';
    
    // Get selected toppings count
    var toppingsCount = 0;
    document.querySelectorAll('#customizationFormDetail input[name="toppings"]:checked').forEach(function(cb) {
        toppingsCount++;
    });
    
    // Calculate additional price
    var additionalPrice = 0;
    if (size === 'Large') additionalPrice += 20;
    if (size === 'Extra Large') additionalPrice += 35;
    if (toppingsCount > 0) additionalPrice += toppingsCount * 15;
    
    var total = basePrice + additionalPrice;
    var totalElement = document.getElementById('detailTotal');
    if (totalElement) totalElement.textContent = '₱' + total.toFixed(2);
}

// Quantity controls for detail modal
function detailIncreaseQty() {
    var qtyInput = document.getElementById('detailQty');
    if (qtyInput) {
        var currentQty = parseInt(qtyInput.value) || 1;
        qtyInput.value = currentQty + 1;
    }
}

function detailDecreaseQty() {
    var qtyInput = document.getElementById('detailQty');
    if (qtyInput) {
        var currentQty = parseInt(qtyInput.value) || 1;
        if (currentQty > 1) {
            qtyInput.value = currentQty - 1;
        }
    }
}

// Add item directly to cart (non-customizable items)
function addToCartDirect(itemId, itemName, itemPrice) {
    var form = document.createElement('form');
    form.method = 'POST';
    form.action = '/Cart/Add';
    
    var idInput = document.createElement('input');
    idInput.type = 'hidden';
    idInput.name = 'id';
    idInput.value = itemId;
    form.appendChild(idInput);
    
    var nameInput = document.createElement('input');
    nameInput.type = 'hidden';
    nameInput.name = 'name';
    nameInput.value = itemName;
    form.appendChild(nameInput);
    
    var priceInput = document.createElement('input');
    priceInput.type = 'hidden';
    priceInput.name = 'price';
    priceInput.value = itemPrice;
    form.appendChild(priceInput);
    
    document.body.appendChild(form);
    form.submit();
}

// Add customized item to cart
function addCustomizedToCart() {
    var itemId = document.getElementById('detailItemId').value;
    var itemName = document.getElementById('detailItemName').value;
    var basePrice = parseFloat(document.getElementById('detailItemPrice').value) || 0;
    
    // Get selected options
    var sizeInput = document.querySelector('input[name="size"]:checked');
    var size = sizeInput ? sizeInput.value : 'Regular';
    
    var sweetnessInput = document.getElementById('detailSweetness');
    var sweetness = sweetnessInput ? parseInt(sweetnessInput.value) : 100;
    
    var iceInput = document.querySelector('input[name="iceLevel"]:checked');
    var iceLevel = iceInput ? iceInput.value : 'Regular';
    
    var specialInput = document.getElementById('detailInstructions');
    var specialInstructions = specialInput ? specialInput.value : '';
    
    var qtyInput = document.getElementById('detailQty');
    var quantity = qtyInput ? parseInt(qtyInput.value) || 1 : 1;
    
    // Get selected toppings
    var toppings = [];
    document.querySelectorAll('#customizationFormDetail input[name="toppings"]:checked').forEach(function(cb) {
        toppings.push(cb.value);
    });
    var toppingsString = toppings.join(', ');
    
    // Calculate additional price
    var additionalPrice = 0;
    if (size === 'Large') additionalPrice += 20;
    if (size === 'Extra Large') additionalPrice += 35;
    if (toppings.length > 0) additionalPrice += toppings.length * 15;
    
    var finalPrice = basePrice + additionalPrice;
    
    // Create form and submit
    var form = document.createElement('form');
    form.method = 'POST';
    form.action = '/Cart/AddWithCustomization';
    
    // Add hidden inputs
    var fields = {
        'id': itemId,
        'name': itemName,
        'price': finalPrice.toFixed(2),
        'quantity': quantity,
        'size': size,
        'sweetness': sweetness,
        'iceLevel': iceLevel,
        'toppings': toppingsString,
        'specialInstructions': specialInstructions,
        'isCustomizable': 'true'
    };
    
    for (var key in fields) {
        var input = document.createElement('input');
        input.type = 'hidden';
        input.name = key;
        input.value = fields[key];
        form.appendChild(input);
    }
    
    document.body.appendChild(form);
    form.submit();
}

// Sweetness slider handler
$(document).ready(function() {
    $('#detailSweetness').on('input', function() {
        var sweetnessText = document.getElementById('sweetnessText');
        if (sweetnessText) {
            sweetnessText.textContent = $(this).val() + '%';
        }
    });
    
    // Update total when options change
    $('input[name="size"]').on('change', updateDetailTotal);
    $('input[name="toppings"]').on('change', updateDetailTotal);
    
    // Category filter buttons - using event delegation for dynamically rendered elements
    $(document).on('click', '.tab-btn', function(e) {
        e.preventDefault();
        var category = $(this).data('category');
        
        // Update active button
        $('.tab-btn').removeClass('active');
        $(this).addClass('active');
        
        // Filter menu cards
        $('.menu-card').each(function() {
            var cardCategory = $(this).data('category');
            
            if (category === 'all' || cardCategory === category) {
                $(this).fadeIn(300);
            } else {
                $(this).fadeOut(300);
            }
        });
    });
});
