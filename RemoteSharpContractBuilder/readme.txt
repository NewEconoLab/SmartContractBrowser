这是用来服务器编译c#智能合约的服务器项目

启动项目为
remotebuilderCore

已知问题：部署会找不到libuv，你能编译的话，这个目录里搜索一定能找到。挑个和平台一致的copy上去。

api说明


http://118.31.39.242:81/_api/help 无参数，仅用来判断服务器是否存活
	
	//部分ajax 代码
	var address = 'http://localhost:81/_api/';
    var xhr: XMLHttpRequest = new XMLHttpRequest();
    xhr.open("GET", address + 'help');
    xhr.onreadystatechange = (ev) => {
    xhr.send();

http://118.31.39.242:81/_api/parse 无参数，用post方式传上来一个名为file的文件块

	//部分ajax代码
	var address = 'http://localhost:81/_api/';
	export function file_str2blob(string: string): Blob {
        var u8 = new Uint8Array(stringToUtf8Array(string));
        var blob = new Blob([u8]);
        return blob;
    }
	var xhr: XMLHttpRequest = new XMLHttpRequest();
    xhr.open("POST", address + 'parse');
    xhr.onreadystatechange = (ev) => {
    var fdata = new FormData();
    fdata.append("file", localsave.file_str2blob(editor.getValue()));
    xhr.send(fdata);

http://118.31.39.242:81/_api/get?hash=0x181778a74103a36c443d52cd1d18338ceb337bac 获取编译结果，hash就是avm的hash

没有例子