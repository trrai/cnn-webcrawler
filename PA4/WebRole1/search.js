(function () {

    "use strict";

    window.onload = function () {
        var input = $("#search_input");
        var submit = $("#search_button");

        //on each keystroke
        $("#search_input").keyup(function (e) {
          
            if (input.val() !== "") {
                //handle casing issues by making first character uppercase and rest lowercase
                handleWikiJSON(input.val().charAt(0).toUpperCase() + input.val().slice(1).toLowerCase());
                console.log(input.val());
            }
        });

        $('#search_button').click(function () {
            handleSubmit(input.val());
        })

    };

    window.receiver_callback = function (data) {
      
        $("#nba_results").show();
        $("#player_name").text(data.name);
        $("#player_team").text(data.team);
        $("#player_gp").text(data.gp);
        $("#player_ppg").text(data.ppg);
        $("#player_threes").text(data.threes);
        $("#player_reb").text(data.reb);
        $("#player_ast").text(data.asts);
        console.log(data.imgLink);
        $("#player_image").attr("src", data.imgLink);
       
    }

    //function to send the ajax request and append the data appropriately 
    function handleWikiJSON(sInput) {

        $.ajax({
            type: "POST",
            url: "Admin.asmx/Search",
            data: JSON.stringify({ input: sInput }),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            async: true,
            success: function (msg) {

                $('#suggestions').empty();
                //if results are not returned 
                if (!eval(msg)["d"]) {
                    $("#suggestions").append("<p> No results found </p>");
                }
                //otherwise append the results found
                else {
                    for (var i in eval(msg)["d"]) {
                        $("#suggestions").append("<p>" + eval(msg)["d"][i] + "</p>");
                    }
                }
            },
            error: function (msg) {
                console.log("error: " + msg);
            }
        });
    };

    function handleSubmit(sInput) {
        $("#search_results").empty();
        $("#loading").show();
        $("#no_result").hide();

        $.ajax({
            type: "GET",
            url: "Admin.asmx/SearchResults",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            async: true,
            data: {input: '"' + sInput + '"'}, 
            success: function (msg) {
                console.log ("request sent success")
                //if results are not returned
                //console.log(msg["d"])
                var results = JSON.parse(msg.d);
                console.log(results);
                $("#search_results").empty();

                if (results.length == 0) {
                    $("#no_result").show();
                }

                //otherwise append the results found
                else {
                    $("#no_result").hide();

                    for (var i in results) {

                        //console.log("Results: " + results[i]);

                        var currentElement = results[i];
                        
                        var card = document.createElement("div");
                        card.className = "card m-3 p-2";
                        card.style.display = "inline-block"
                        card.style.minWidth = "500px";
                        
                        card.style.width = "500px";

                        var img = document.createElement("img");
                        img.className = "card-img-top";
                        img.setAttribute('src', currentElement["Item6"]);

                        var cardBlock = document.createElement("div");
                        cardBlock.className = "card-block";

                        var title = document.createElement("h4");
                        title.appendChild(document.createTextNode(currentElement["Item2"]));
                        title.className = "card-title";

                        var text = document.createElement("p");
                        var elementTextArr = currentElement["Item4"].split(" ");
                        var shortenedTextContent = elementTextArr.splice(0, 50).join(" ");
                        var inputWords = sInput.split(" ");

                        var subtitle = document.createElement("h6");
                        subtitle.appendChild(document.createTextNode(currentElement["Item5"]));
                        subtitle.className = "card-subtitle mb-2 text-muted";

                        //bold query words
                        for (var i = 0; i < inputWords.length; i++) {
                            console.log("input word: " + inputWords[i]);
                            shortenedTextContent = shortenedTextContent.replace(" " + inputWords[i] + " ", " <strong>" + inputWords[i] + "</strong> ");
                            console.log("replaced with: " + "<strong> " + inputWords[i] + " </strong>");
                        }

                        shortenedTextContent += "...";
                        text.innerHTML = "<p>" + shortenedTextContent + "</p>";
                        text.className = "card-text";
                        
                        var link = document.createElement("a");
                        link.setAttribute('href', currentElement["Item3"]);
                        link.setAttribute('target', "_blank");
                        link.className = "btn blue-button";
                        link.appendChild(document.createTextNode("Visit"));

                        cardBlock.appendChild(title);
                        cardBlock.appendChild(subtitle);
                        cardBlock.appendChild(text);
                        cardBlock.appendChild(link);
                        card.appendChild(img);
                        card.appendChild(cardBlock);


                        $('#search_results').append(card);

                        //$("#search_results").append("<tr><td>" + stats[0] +
                          //  "</td><td><a  target='_blank' href=" + stats[1] + "> Click here to visit! </a></td>" + 
                            //"<td><img style='width:40%' src='" + stats[2] + "' /> </td> </tr>");
                    }

                    

                }
                $("#loading").hide();
            },
            error: function (msg) {
                console.log("error: " + msg.responseText);
            }
        });

       
        $.ajax({
            crossDomain: true,
            url: "http://ec2-18-221-154-163.us-east-2.compute.amazonaws.com/search.php",
            data: {search: sInput},
            contentType: "application/json; charset=utf-8",
            dataType: "jsonp",
            async: true,
            jsonpCallback: "receiver_callback",
            success: function (data) {
                $("#loading").hide();
                console.log("success");
                console.log(data);
            },
            error: function (msg) {
                $("#nba_results").hide();
            }
        });
    };


})();