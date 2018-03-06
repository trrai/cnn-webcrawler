(function() {


    $(document).ready(function () {
        // run the first time; all subsequent calls will take care of themselves
        performanceRequest();
        fetchLastTenRequest();
        errorRequest();
        makeChart();
        countRequest();
        getSuggestionStats();

        //refresh automatically every X seconds
        setInterval(performanceRequest, 1000);
        setInterval(fetchLastTenRequest, 5000);
        setInterval(errorRequest, 5000);
        setInterval(updateStatus, 2000);
        setInterval(getSuggestionStats, 2000);
        setInterval(countRequest, 1000);


        //Get buttons
        var fetchLastTenButton = document.getElementById('fetchLastTen_button');
        var performanceButton = document.getElementById('performance_button');
        var startButton = document.getElementById('start');
        var stopButton = document.getElementById('stop');
        var clearButton = document.getElementById('clear_button');
        var trieButton = document.getElementById('trie_button')
        var updateChartButton = document.getElementById('update_chart');
        var errorsButton = document.getElementById('errors_button');

        //Add listeners
        fetchLastTenButton.addEventListener('click', function () {
            fetchLastTenRequest();
        });

        performanceButton.addEventListener('click', function () {
            performanceRequest();
        });

        errorsButton.addEventListener('click', function () {
            errorRequest();
        });

        startButton.addEventListener('click', function () {
            startRequest();
        })

        stopButton.addEventListener('click', function () {
            stopRequest();
        })

        trieButton.addEventListener('click', function () {
            $('#trie_button').attr('disabled', true);
            $('#trie_button').text("Building... Please Wait");
            buildTrieRequest();
        })

        clearButton.addEventListener('click', function () {
            $('#clear_button').attr('disabled', true);
            $('#clear_button').text("Clearing... Please Wait");
            clearRequest();
        })
        updateChartButton.addEventListener('click', function () {
            updateChart();
        })

        
    });
    var cpuInitData = [];
    var memInitData = [];

    //function to clear queues, storage, etc.
    function clearRequest() {
        $.ajax({
            type: "POST",
            url: "Admin.asmx/ClearAll",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                console.log("CLEARED!");
                $('#clear_button').attr('disabled', false);
                $('#clear_button').text("Clear All");
            },
            error: function (msg) {
                console.log("error: " + msg);
            }
        });
    }
    //function to build the trie for suggestions
    function buildTrieRequest() {

        $.ajax({
            type: "POST",
            url: "Admin.asmx/BuildTrie",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                console.log("BUILT TRIE!");
                $('#trie_button').attr('disabled', false);
                $('#trie_button').text("Build Trie");
            },
            error: function (msg) {
                console.log("error: " + msg);
            }
        });
    }
    //function to get the current counts of queue and table
    function countRequest() {
        $.ajax({
            type: "POST",
            url: "Admin.asmx/GetCount",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                console.log("Success");
                //if results are not returned
                if (!eval(msg)["d"]) {
                    $("#count_stats").text("No Count Found... Please wait")
                }
                //otherwise append the results found
                else {
                    var stats = eval(msg)["d"].replace(/['"]+/g, '').split("|");
                    $('#queue_count').text(stats[0]);
                    $('#table_count').text(stats[1]);
                }

            },
            error: function (msg) {
                console.log("error: " + msg);
            }
        });
    }
    //function to update chart
    function updateChart() {
        $.ajax({
            type: "POST",
            url: "Admin.asmx/GetPerformanceChartData",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                console.log("Success");
                //if results are not returned
                if (!eval(msg)["d"]) {
                    $("#myChart").append("<p> No results found </p>");
                }
                //otherwise append the results found
                else {
                    console.log("Updating chart..");
                    var cpuData = [];
                    var memData = [];
                    for (var i in eval(msg)["d"]) {
                        var stats = eval(msg)["d"][i].split("|");
                        cpuData.push(stats[0]);
                        memData.push(stats[1] / 100);
                    }

                    config.data.datasets = [{
                        label: "CPU",
                        backgroundColor: '#FF0000',
                        borderColor: '#FF0000',
                        data: cpuData,
                        fill: false,
                    }, {
                        label: "RAM",
                        fill: false,
                        backgroundColor: '#008000',
                        borderColor: '#008000',
                        data: memData,
                    }];

                    window.myLineChart.update();
                }

            },
            error: function (msg) {
                console.log("error: " + msg);
            }
        });
    }
    var config = {
        type: 'line',
        data: {
            labels: ["1", "2", "3", "4", "5", "6", "7", "8", "9", "10"],
            datasets: [{
                label: "CPU",
                backgroundColor: '#FF0000',
                borderColor: '#FF0000',
                data: cpuInitData,
                fill: false,
            }, {
                label: "RAM",
                fill: false,
                backgroundColor: '#008000',
                borderColor: '#008000',
                data: memInitData,
            }]
        },
        options: {
            responsive: true,
            title: {
                display: true,
                text: 'Performance'
            },
            tooltips: {
                mode: 'index',
                intersect: false,
            },
            hover: {
                mode: 'nearest',
                intersect: true
            },
            scales: {
                xAxes: [{
                    display: true,
                    scaleLabel: {
                        display: true,
                        fontSize: 18,
                        labelString: 'Time Interval'
                    },
                    ticks: {
                        fontSize: 15
                    }
                }],
                yAxes: [{
                    display: true,
                    scaleLabel: {
                        display: true,
                        fontSize: 18,
                        labelString: 'Value (RAM x100)'
                    },
                    ticks: {
                        fontSize: 15
                    }

                }]
            }
        }
    };
    //function to create chart
    function makeChart() {
        $.ajax({
            type: "POST",
            url: "Admin.asmx/GetPerformanceChartData",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                console.log("Success");
                //if results are not returned
                if (!eval(msg)["d"]) {
                    $("#myChart").append("<p> No results found </p>");
                }
                //otherwise append the results found
                else {

                    for (var i in eval(msg)["d"]) {
                        var stats = eval(msg)["d"][i].split("|");
                        cpuInitData.push(stats[0]);
                        memInitData.push(stats[1] / 100);
                    }

                    var ctx = document.getElementById("myChart").getContext("2d");
                    window.myLineChart = new Chart(ctx, config);

                }

            },
            error: function (msg) {
                console.log("error: " + msg);
            }
        });
    }
    //function to update status
    function updateStatus() {

        $.ajax({
            type: "POST",
            url: "Admin.asmx/GetStatus",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                $('#status').text(eval(msg)["d"].replace(/['"]+/g, ''));
            },
            error: function (msg) {
                console.log("error: " + msg);
            }
        });
    };
    //function to update suggestion stats
    function getSuggestionStats() {
        $.ajax({
            type: "POST",
            url: "Admin.asmx/GetSuggestionStats",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                
                //if results are not returned
                if (eval(msg)["d"] == "No stats available") {
                    $("#suggestion_count").text("N/A...Trie not built");
                    $("#last_suggestion").text("N/A...Trie not built");
                }
                //otherwise append the results found
                else {
                    var stats = eval(msg)["d"].replace(/['"]+/g, '').split("|");
                    
                    $('#suggestion_count').text(stats[1]);
                    $('#last_suggestion').text(stats[0]);
                }
            },
            error: function (msg) {
                console.log("error: " + msg);
            }
        });
    }
    //function to start
    function startRequest() {

        $.ajax({
            type: "POST",
            url: "Admin.asmx/StartCrawl",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                console.log("STARTED!");

            },
            error: function (msg) {
                console.log("error: " + msg);
            }
        });
    };
    //function to stop
    function stopRequest() {

        $.ajax({
            type: "POST",
            url: "Admin.asmx/StopCrawl",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                console.log("STOPPED!");

            },
            error: function (msg) {
                console.log("error: " + msg);
            }
        });
    };
    //function to send performance request
    function performanceRequest() {

        $.ajax({
            type: "POST",
            url: "Admin.asmx/GetPerformance",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                console.log("Success");
                $('#performance_results').empty();
                //if results are not returned
                if (!eval(msg)["d"]) {
                    $("#performance_results").append("<p> No results found </p>");
                }
                //otherwise append the results found
                else {
                    var index = 1;
                    for (var i in eval(msg)["d"]) {
                        $("#performance_results").append("<li class='list-group-item'>" + eval(msg)["d"][i] + "</li>");
                        index++;
                    }
                }

            },
            error: function (msg) {
                console.log("error: " + msg);
            }
        });
    };
    //function to send errors table request
    function errorRequest() {

        $.ajax({
            type: "POST",
            url: "Admin.asmx/GetErrors",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                console.log("Success");
                $('#error_results').empty();
                //if results are not returned
                if (!eval(msg)["d"]) {
                    $("#error_results").append("<p> No results found </p>");
                }
                //otherwise append the results found
                else {
                    var index = 1;
                    for (var i in eval(msg)["d"]) {
                        $("#error_results").append("<li class='list-group-item'>" + eval(msg)["d"][i] + "</li>");
                        index++;
                    }
                }

            },
            error: function (msg) {
                console.log("error: " + msg);
            }
        });
    };
    //function to send the ajax request and append the data appropriately
    function fetchLastTenRequest() {

        $.ajax({
            type: "POST",
            url: "Admin.asmx/GetLast10Added",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (msg) {
                console.log("Success");
                $('#results').empty();
                //if results are not returned
                if (!eval(msg)["d"]) {
                    $("#results").append("<p> No results found </p>");
                }
                //otherwise append the results found
                else {
                    var index = 1;
                    for (var i in eval(msg)["d"]) {
                        $("#results").append("<li class='list-group-item'>" + index.toString() + ". " +
                            "<a target='_blank' href= '" + eval(msg)["d"][i] + "'>" + eval(msg)["d"][i] + "</li>");
                        index++;
                    }
                }

            },
            error: function (msg) {
                console.log("error: " + msg);
            }
        });
    };

})();