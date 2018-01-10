var localsave;
(function (localsave) {
    function stringToUtf8Array(str) {
        var bstr = [];
        for (var i = 0; i < str.length; i++) {
            var c = str.charAt(i);
            var cc = c.charCodeAt(0);
            if (cc > 0xFFFF) {
                throw new Error("InvalidCharacterError");
            }
            if (cc > 0x80) {
                if (cc < 0x07FF) {
                    var c1 = (cc >>> 6) | 0xC0;
                    var c2 = (cc & 0x3F) | 0x80;
                    bstr.push(c1, c2);
                }
                else {
                    var c1 = (cc >>> 12) | 0xE0;
                    var c2 = ((cc >>> 6) & 0x3F) | 0x80;
                    var c3 = (cc & 0x3F) | 0x80;
                    bstr.push(c1, c2, c3);
                }
            }
            else {
                bstr.push(cc);
            }
        }
        return bstr;
    }
    localsave.stringToUtf8Array = stringToUtf8Array;
    function file_str2blob(string) {
        var u8 = new Uint8Array(stringToUtf8Array(string));
        var blob = new Blob([u8]);
        return blob;
    }
    localsave.file_str2blob = file_str2blob;
})(localsave || (localsave = {}));
window.onload = function () {
    function syntaxHighlight(json) {
        if (typeof (json) != 'string') {
            json = JSON.stringify(json, null, 4);
            //return json;
        }
        json = json.replace(/&/g, '&').replace(/</g, '<').replace(/>/g, '>');
        return json.replace(/("(\\u[a-zA-Z0-9]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(true|false|null)\b|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?)/g, function (match) {
            var cls = 'number';
            if (/^"/.test(match)) {
                if (/:$/.test(match)) {
                    cls = 'key';
                }
                else {
                    cls = 'string';
                }
            }
            else if (/true|false/.test(match)) {
                cls = 'boolean';
            }
            else if (/null/.test(match)) {
                cls = 'null';
            }
            if (cls == 'key') {
                return '<span class="' + cls + '">' + match + '</span>';
            }
            else {
                return '<span class="' + cls + '">' + match + '</span><br/>';
            }
        });
    }
    var csharpcode = [
        'using Neo.SmartContract.Framework;',
        'using Neo.SmartContract.Framework.Services.Neo;',
        'using Neo.SmartContract.Framework.Services.System;',
        '',
        'class A : SmartContract',
        '{',
        '    public static int Main() ',
        '    {',
        '        return 1;',
        '    }',
        '}',
    ].join('\n');
    var javacode = [
        'package hi;',
        'import org.neo.smartcontract.framework.*;',
        'public class go extends SmartContract {',
        '    public static int Main() ',
        '    {',
        '        return 1;',
        '    }',
        '}',
    ].join('\n');
    var editor = monaco.editor.create(document.getElementById('container'), {
        value: csharpcode,
        language: 'csharp',
        theme: 'vs-dark'
    });
    //var address = 'http://localhost:82/_api/';
    var address = 'http://47.96.168.8:82/_api/';
    //var address = 'http://40.125.201.127:81/_api/';
    //var address = 'http://localhost:81/_api/';
    {
        var xhr = new XMLHttpRequest();
        xhr.open("GET", address + 'help');
        xhr.onreadystatechange = function (ev) {
            if (xhr.readyState == 4) {
                $('#info').html(syntaxHighlight(JSON.parse(xhr.responseText)));
                //var txt = document.getElementById('info') as HTMLSpanElement;
                //txt.innerText = syntaxHighlight(xhr.responseText);
            }
        };
        xhr.send();
    }
    var btn = document.getElementById('doit');
    btn.onclick = function (ev) {
        var xhr = new XMLHttpRequest();
        xhr.open("POST", address + 'parse');
        xhr.onreadystatechange = function (ev) {
            if (xhr.readyState == 4) {
                var J = JSON.parse(xhr.responseText);
                J["funcsigns"] = JSON.parse(J["funcsigns"]);
                $('#info').html(syntaxHighlight(J));
                //var txt = document.getElementById('info') as HTMLSpanElement;
                //txt.innerText = syntaxHighlight(xhr.responseText);
            }
        };
        var fdata = new FormData();
        fdata.append("language", "csharp");
        fdata.append("requestIP", $('#ip').val());
        fdata.append("file", localsave.file_str2blob(editor.getValue()));
        xhr.send(fdata);
    };
};
//# sourceMappingURL=app.js.map