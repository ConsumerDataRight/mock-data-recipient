function AppendCdrArrangementIdToRequest(req) {
    if (req.loadSpec) return req;

    var select = document.getElementById("select-cdr-arrangement-id");
    req.headers["x-inject-cdr-arrangement-id"] = select.value;

    if (req.headers["x-fapi-auth-date"] == null) {
        req.headers["x-fapi-auth-date"] = "Fri, 01 Jan 2021 00:00:00 GMT";
    }

    return req;
}

async function AddCdrArrangementSelector() {
    var div = document.createElement("div");
    div.className = "form";

    var select = document.createElement("select");
    select.id = "select-cdr-arrangement-id";
    select.className = "form-control";
    select.placeholder = "Select CDR Arrangement...";

    var emptyOption = document.createElement("option");
    emptyOption.value = "";
    emptyOption.text = "Select CDR Arrangement...";
    select.appendChild(emptyOption);

    var arrangements = await GetArrangements();

    //Create and append the options
    if (arrangements !== null && typeof arrangements !== 'undefined') {
        for (var item of arrangements) {
            var option = document.createElement("option");
            option.value = item["key"];
            option.text = item["value"];
            select.appendChild(option);
        }
    }

    var parent = document.getElementById("swagger-ui");
    parent.before(div);
    div.appendChild(select);
}

async function GetArrangements() {
    let url = GetPath() + '/cdr-arrangements';
    var data;

    await fetch(url)
        .then(res => res.json())
        .then((out) => {
            data = out;
        })
        .catch(err => { throw err });

    return data;
}

function GetPath() {
    if (this.location.href.indexOf("banking") >= 0) {
        return "/data-sharing-banking"
    }

    return "/data-sharing-energy"
}

document.addEventListener("DOMContentLoaded", function (event) {
    AddCdrArrangementSelector();
});
