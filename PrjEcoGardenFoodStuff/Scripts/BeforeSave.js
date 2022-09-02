
var compID;
var sessionId = 0;
var baseUrl = "http://localhost/PrjEcoGardenFoodStuff/";
var VoucherNo;
var requestId = 0;
var requestsProcessed = [];
var bodyRequestsProcessed = [];

function BeforeSaveFn() {
    debugger;
    Focus8WAPI.getGlobalValue("VoucherCallback", '*', 1);
}
function AfterSaveFn() {
    debugger;
    Focus8WAPI.getGlobalValue("VoucherCallback2", '*', 1);
}
function VoucherCallback(response) {
    debugger;
    compID = response.data.CompanyId;
    sessionId = response.data.SessionId;
    Focus8WAPI.getFieldValue("setAfterCallback", ["", "DocNo", "Consignment Batch No"], Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, false, requestId);
}
function VoucherCallback2(response) {
    debugger;
    compID = response.data.CompanyId;
    sessionId = response.data.SessionId;
    Focus8WAPI.getFieldValue("setAfterCallback2", ["", "DocNo", "Consignment Batch No"], Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, false, requestId);
}

function setAfterCallback(response) {
    debugger;
    if (isRequestCompleted(response.iRequestId, requestsProcessed)) {
        return;
    }
    requestsProcessed.push(response.iRequestId);
    console.log(response);
    VoucherNo = response.data[1].FieldValue;
    Focus8WAPI.getBodyFieldValue('RetriveValues', ["", "*"], 2, false, 1, requestId++);
}
function setAfterCallback2(response) {
    debugger;
    if (isRequestCompleted(response.iRequestId, requestsProcessed)) {
        return;
    }
    requestsProcessed.push(response.iRequestId);
    console.log(response);
    VoucherNo = response.data[1].FieldValue;
    PostConsignmentBatchNo();
}
function isRequestCompleted(iRequestId, requestsArray) {
    debugger;
    requestsArray.indexOf(iRequestId) === -1 ? false : true;
}
function setFieldValueCallBack(response) {
    debugger
    requestsProcessed.push(response.iRequestId);
    console.log('Callback :: setFieldValueCallBack :: Response : ', response);
}
function setBodyFieldValueCallBack(response) {
    debugger
    requestsProcessed.push(response.iRequestId);
    console.log('Callback :: setBodyFieldValueCallBack :: Response : ', response);
}
function RetriveValues(response) {
    debugger;
    if (isRequestCompleted(response.iRequestId, requestsProcessed)) {
        return;
    }
    requestsProcessed.push(response.iRequestId);
    isvalidrows = response.RowsInfo.iValidRows;
    vsBodyData = {};
    vsBodyDataArr = [];
    vsBodyDataArr.length = 0;
    bodyRequestsProcessed = [];
    if (isvalidrows > 0) {

        for (let i = 1; i <= isvalidrows; i++) {
            Focus8WAPI.getBodyFieldValue('initializeRow', ["*"], Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, false, i, i);
        }

    }
}

function isRequestCompleted(iRequestId, requestsArray) {
    debugger;
    requestsArray.indexOf(iRequestId) === -1 ? false : true;
}
function initializeRow(response) {
    debugger;
    try {
        if (isRequestCompleted(response.iRequestId, bodyRequestsProcessed)) {
            return;
        }
        bodyRequestsProcessed.push(response.iRequestId);
        debugger;
        const row = initializeRowDataFields(response.data.slice(0));
        vsBodyDataArr.push(row);
        vsBodyData[response.iRequestId] = row;
        requestId = 0;
        var i = 0;
        if (isvalidrows === Object.values(vsBodyData).length) {
            debugger;
            for (var j = 0; j < isvalidrows; j++) {
                debugger;
                requestId = j + 1;
                console.log(requestId)
                i = j + 1;
                console.log(i)
                Focus8WAPI.setBodyFieldValue('setBodyFieldValueCallBack', ["Batch"], [VoucherNo], Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, false, i, requestId);
            }
            Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, true);
        }

    } catch (e) {
        alert(e.message)
    }
}
function initializeRowDataFields(fields) {
    debugger;
    const row = {};
    Object.values(fields).forEach((v, i, a) => {
        if (v) {
            row[`${v['sFieldName']}`] = {};
            row[`${v['sFieldName']}`]['sFieldName'] = v['sFieldName'];
            row[`${v['sFieldName']}`]['FieldText'] = v['FieldText'];
            row[`${v['sFieldName']}`]['FieldValue'] = v['FieldValue'];
            row[`${v['sFieldName']}`]['iFieldId'] = v['iFieldId'];
        }

    })
    console.log('rowDataM   string:: ', JSON.stringify(row));
    return row;
}

function PostConsignmentBatchNo() {
    debugger;
    $.ajax({
        type: "get",
        url: baseUrl + "Home/Index?companyId=" + compID + "&VoucherNo=" + VoucherNo,
        datatype: "JSON",
        contentType: "application/json; charset=utf-8",
        success: function (result) {
            debugger;
            console.log("Success");
            console.log(result);
            if (result == "Success") {
                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, true);
            }
            else {
                alert("Consignment Batch No Master Creation Failed");
                Focus8WAPI.continueModule(Focus8WAPI.ENUMS.MODULE_TYPE.TRANSACTION, true);
            }
        },
        error: function (err) {
            console.log("Error");
            console.log(err);
        }
    });
}


