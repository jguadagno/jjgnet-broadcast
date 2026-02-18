$(function(){
    $("#ItemTableName").change(function () {
       let selectedTable = $(this).val(); 
       let primaryKey = $("#ItemPrimaryKey");
       switch (selectedTable) {
           case "Engagements":
           case "Talks":
           case "SyndicationFeed":
           case "YouTube":
           default:
               primaryKey.prop("readonly", false);
               break;
       }
           
    });
});
