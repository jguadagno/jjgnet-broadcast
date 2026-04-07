$(function(){
    // Sync ItemType enum to ItemTableName (for backward compatibility)
    $("#ItemType").change(function () {
        let selectedType = $(this).val();
        let tableNameMap = {
            "0": "Engagements",
            "1": "Talks",
            "2": "SyndicationFeedSources",
            "3": "YouTubeSources"
        };
        $("#ItemTableName").val(tableNameMap[selectedType] || "");
        // Clear validation result when type changes
        $("#validation-result").html("");
    });
    
    // Validate item when button is clicked
    $("#btnValidateItem").click(function() {
        validateScheduledItem();
    });
    
    // Also validate on Enter key in ItemPrimaryKey field
    $("#ItemPrimaryKey").keypress(function(e) {
        if (e.which === 13) { // Enter key
            e.preventDefault();
            validateScheduledItem();
        }
    });
    
    function validateScheduledItem() {
        let itemType = $("#ItemType").val();
        let itemPrimaryKey = $("#ItemPrimaryKey").val();
        let resultDiv = $("#validation-result");
        
        // Clear previous result
        resultDiv.html("");
        
        // Validate inputs
        if (!itemType) {
            resultDiv.html('<div class="alert alert-warning"><i class="bi bi-exclamation-triangle me-2"></i>Please select an item type first.</div>');
            return;
        }
        
        if (!itemPrimaryKey || itemPrimaryKey <= 0) {
            resultDiv.html('<div class="alert alert-warning"><i class="bi bi-exclamation-triangle me-2"></i>Please enter a valid item ID.</div>');
            return;
        }
        
        // Show loading spinner
        resultDiv.html('<div class="alert alert-info"><span class="spinner-border spinner-border-sm me-2"></span>Validating...</div>');
        
        // Call validation endpoint
        $.ajax({
            url: '/Schedules/ValidateItem',
            type: 'GET',
            data: { 
                itemType: itemType, 
                itemPrimaryKey: itemPrimaryKey 
            },
            success: function(result) {
                if (result.isValid) {
                    let html = '<div class="alert alert-success">' +
                        '<i class="bi bi-check-circle-fill me-2"></i>' +
                        '<strong>Found:</strong> ' + result.itemTitle;
                    
                    if (result.itemDetails) {
                        html += '<br/><small class="text-muted">' + result.itemDetails + '</small>';
                    }
                    
                    html += '</div>';
                    resultDiv.html(html);
                } else {
                    resultDiv.html(
                        '<div class="alert alert-danger">' +
                        '<i class="bi bi-x-circle-fill me-2"></i>' +
                        '<strong>Not Found:</strong> ' + result.errorMessage +
                        '</div>'
                    );
                }
            },
            error: function(xhr, status, error) {
                resultDiv.html(
                    '<div class="alert alert-warning">' +
                    '<i class="bi bi-exclamation-triangle-fill me-2"></i>' +
                    'Unable to validate item. Please try again.' +
                    '</div>'
                );
            }
        });
    }
});
