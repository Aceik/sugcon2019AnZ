var sellableItems;

function CreateContact() {
    var api_url = 'https://playground.sc/api/contactapi/Contact';
    var result = [];
    $.ajax({
        url: api_url,
        type: 'POST',
        contentType: 'application/x-www-form-urlencoded',
        data: result,
        dataType: 'json',
        success: function (result) {
            var template = $('#CreateContactResult').html();
            Mustache.parse(template);   // optional, speeds up future uses
            result.getProductName = getProductName;
            var rendered = Mustache.render(template, result);
            $('#CreateContactResultBox').html(rendered);
        }
    });
}

function RegisterTask() {
    var api_url = 'https://playground.sc/api/contactapi/RegisterTasks';
    var result = [];
    $.ajax({
        url: api_url,
        type: 'POST',
        contentType: 'application/x-www-form-urlencoded',
        data: result,
        dataType: 'json',
        success: function (result) {
            $('#TaskRegisterResult').text("");
            for (var i = 0; i < result.length; i++) {
                switch (i) {
                    case 0:
                        $('#TaskRegisterResult').append("projectionTaskId: " + result[i] + "<br>");
                        break;
                    case 1:
                        $('#TaskRegisterResult').append("mergeTaskId: " + result[i] + "<br>");
                        break;
                    case 2:
                        $('#TaskRegisterResult').append("recommendationTaskId: " + result[i] + "<br>");
                        break;
                    case 3:
                        $('#TaskRegisterResult').append("storeFacetTaskId: " + result[i] + "<br>");
                        break;
                }
            }
            $('#TaskRegisterResultBox').css("border-style", "double");
        }
    });
}

function ShowRecommendation(productId) {
    var api_url = 'https://playground.sc/api/contactapi/Recommendtion?id=' + productId;
    var result = [];
    $.ajax({
        url: api_url,
        type: 'GET',
        contentType: 'application/x-www-form-urlencoded',
        data: result,
        dataType: 'json',
        accepts: 'application/json',
        success: function (result) {
            var template = $('#ShowRecommendationSingleResult').html();
            Mustache.parse(template);   // optional, speeds up future uses
            result.getProductName = getProductName;
            var rendered = Mustache.render(template, result);
            $('#ShowRecommendationSingleResultBox').html(rendered);
        }
    });
}

function ShowRecommendations() {
    var api_url = 'https://playground.sc/api/contactapi/Recommendtions';
    var result = {};
    $.ajax({
        url: api_url,
        type: 'GET',
        contentType: 'application/x-www-form-urlencoded',
        data: result,
        dataType: 'json',
        accepts: 'application/json',
        success: function (data) {
            var template = $('#ShowRecommendationResult').html();
            Mustache.parse(template);   // optional, speeds up future uses
            result.data = data;
            result.getProductName = getProductName;
            var rendered = Mustache.render(template, result);
            $('#ShowRecommendationResultBox').html(rendered);
        }
    });
}

function readJson() {
    $.getJSON("/Data/sellableItems.json", function (json) {
        sellableItems=json;
    });
}

function getProductName() {
    return function (val, render) {
        var result = sellableItems.find(p => p.ProductId == render(val).toString());
        return result == undefined ? "N/A" : result.Name;
    };
}