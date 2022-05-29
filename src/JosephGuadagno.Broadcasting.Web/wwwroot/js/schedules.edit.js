$(function(){
    $("#ItemTableName").change(function () {
       let selectedTable = $(this).val(); 
       let primaryKey = $("#ItemPrimaryKey");
       let secondaryKey =$("#ItemSecondaryKey");
       switch (selectedTable) {
           case "Engagements":
               primaryKey.prop("readonly", false);
               secondaryKey.prop("readonly", true);
               secondaryKey.val("");
               break;
           case "Talks":
               primaryKey.prop("readonly", false);
               secondaryKey.prop("readonly", true);
               secondaryKey.val("");
               break;
           case "SyndicationFeed":
               primaryKey.prop("readonly", "true");
               primaryKey.val("SyndicationFeed");
               secondaryKey.prop("readonly", false);
               break;
           case "YouTube":
               primaryKey.prop("readonly", "true");
               primaryKey.val("YouTube");
               secondaryKey.prop("readonly", false);
               break;
           default:
               primaryKey.prop("readonly", false);
               secondaryKey.prop("readonly", false);
               break;
       }
           
    });
});