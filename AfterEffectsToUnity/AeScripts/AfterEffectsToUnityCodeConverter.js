var AfterEffectsToUnity = AfterEffectsToUnity || {};

// 生成するコードの名前空間をプロジェクトに都合がいいように設定してね!
AfterEffectsToUnity.OutputNamespace = "Ae2UnitySample";

// AfterEffects JSON → C#の変換器
(function initializeAfterEffectsToUnity() {
	var self = AfterEffectsToUnity;
	// 公開インターフェイス
	self.Mode = {
		Ugui: 0,
		Unity2d: 1,
		Unknown: -0x7fffffff
	};
	self.dumpAll = false;
	//デシリアライズ(JSON.parse)されたプロジェクトを受け取る
	self.convert = function (project, mode, dumpAll) {
		var out = '';
		out += '// project name : ' + project.name + '\n';
		out += '\n';

		if (project.compositions) {
			// コンポジションの名前→
			var compositionMap = {};
			for (var i = 0; i < project.compositions.length; ++i) {
				compositionMap[project.compositions[i].name] = true;
			}
			for (var i = 0; i < project.compositions.length; ++i) {
				var composition = project.compositions[i];
				// レイヤー入ってないコンポジットは無視
				if (composition.layers.length > 0) {
					// 全レイヤーについてcompositionなのかを判別
					for (var j = 0; j < composition.layers.length; ++j){
						var layer = composition.layers[j];
						layer.isImage = (compositionMap[layer.name] !== true) && layer.enabled;
					}

					if (mode === self.Mode.Ugui) {
						out += convertCompositionUgui(composition);
					} else if (mode === self.Mode.Unity2d) {
						out += convertCompositionUnity2d(composition);
					}
				}
			}
		}
		return out;
	};

	// 以下非公開インターフェイス
	var convertCompositionUgui = function (composition) {
		var mode = self.Mode.Ugui;
		// 不正な名前を無理矢理変換(.を_にするなど)
		modifyLayerNames(composition.layers);
		var nodeArray = [];
		var treeRoot = extractTree(nodeArray, composition.layers);
		var out = writeCompositionHeader(composition, treeRoot, true);
		var i;
		for (i = 0; i < nodeArray.length; ++i) {
			var layer = nodeArray[i].layer;
			out += '\t\t[SerializeField]\n';
			if (layer.isImage){
				out += '\t\tprivate Graphic _' + layer.name + ';\n';
			}else{
				out += '\t\tprivate RectTransform _' + layer.name + ';\n';
			}
		}
		out += '\n';
		// SetFirstFrame
		out += '\t\tprotected override void SetFirstFrame()\n';
		out += '\t\t{\n';
		for (i = 0; i < nodeArray.length; ++i) {
			var node = nodeArray[i];
			out += '\t\t\tAfterEffectsUtil.Set(\n';
			out += '\t\t\t\t_' + node.layer.name + ',\n';
			out += '\t\t\t\t' + node.anchorX + 'f,\n';
			out += '\t\t\t\t' + node.anchorY + 'f,\n';
			out += '\t\t\t\t' + node.positionX + 'f,\n';
			out += '\t\t\t\t' + node.positionY + 'f,\n';
			out += '\t\t\t\t' + node.scaleX + 'f,\n';
			out += '\t\t\t\t' + node.scaleY + 'f,\n';
			if (node.layer.isImage){
				out += '\t\t\t\t' + node.rotation + 'f,\n';
				out += '\t\t\t\t' + node.opacity + 'f);\n';
			}else{
				out += '\t\t\t\t' + node.rotation + 'f);\n';
			}
		}
		out += '\t\t}\n';
		out += '\n';

		var workAreaStart = composition.workAreaStart;
		var workAreaEnd = workAreaStart + composition.workAreaDuration;
		out += writeCreateResource(composition, nodeArray, mode);
		out += writeInitializeInstance(nodeArray, mode, workAreaStart, workAreaEnd);
		out += writeResourceFunctions();
		out += writeBuildHierarchy(treeRoot, composition, mode);
		return out;
	};
	var convertCompositionUnity2d = function (composition) {
		var mode = self.Mode.Unity2d;
		// 不正な名前を無理矢理変換(.を_にするなど)
		modifyLayerNames(composition.layers);
		var nodeArray = [];
		var treeRoot = extractTree(nodeArray, composition.layers);
		var out = writeCompositionHeader(composition, treeRoot, false);
		var i;
		for (i = 0; i < nodeArray.length; ++i) {
			var layer = nodeArray[i].layer;
			out += '\t\t[SerializeField]\n';
			out += '\t\tprivate SpriteRenderer _' + layer.name + ';\n';
		}
		out += '\n';
		// SetFirstFrame
		out += '\t\tprotected override void SetFirstFrame()\n';
		out += '\t\t{\n';
		var ZStep = 0.1; //TODO: 後で受け取れるように
		for (i = 0; i < nodeArray.length; ++i) {
			var node = nodeArray[i];
			out += '\t\t\tAfterEffectsUtil.Set(\n';
			out += '\t\t\t\t_' + node.layer.name + ',\n';
			out += '\t\t\t\t' + ((node.parent) ? -ZStep : 0) + 'f,\n';
			// 根ならピボットはない
			out += '\t\t\t\t';
			if (node.parent.layer === null) {
				out += 'Vector2.zero,\n';
			} else {
				out += '_' + node.parent.name + '.sprite.pivot,\n';
			}
			out += '\t\t\t\t' + node.anchorX + 'f,\n';
			out += '\t\t\t\t' + node.anchorY + 'f,\n';
			out += '\t\t\t\t' + node.positionX + 'f,\n';
			out += '\t\t\t\t' + node.positionY + 'f,\n';
			out += '\t\t\t\t' + node.scaleX + 'f,\n';
			out += '\t\t\t\t' + node.scaleY + 'f,\n';
			out += '\t\t\t\t' + node.rotation + 'f,\n';
			out += '\t\t\t\t' + node.opacity + 'f);\n';
		}
		out += '\t\t}\n';
		out += '\n';

		var workAreaStart = composition.workAreaStart;
		var workAreaEnd = workAreaStart + composition.workAreaDuration;
		out += writeCreateResource(composition, nodeArray, mode);
		out += writeInitializeInstance(nodeArray, mode, workAreaStart, workAreaEnd);
		out += writeResourceFunctions();
		out += writeBuildHierarchy(treeRoot, composition, mode);
		return out;
	};
	var writeCreateResource = function (composition, nodeArray, mode) {
		var out = '';
		out += '\t\tpublic static void CreateResource()\n'
		out += '\t\t{\n';
		out += '\t\t\t_resource = new AfterEffectsResource(' + composition.frameRate + 'f);\n';
		out += '\t\t\t_resource\n';

		var workAreaStart = composition.workAreaStart;
		var workAreaEnd = workAreaStart + composition.workAreaDuration;
		for (i = 0; i < nodeArray.length; ++i) {
			out += writeLayerAnimation(nodeArray[i], mode, workAreaStart, workAreaEnd);
		}

		// 全体カットを追加
		out += '\t\t\t\t.AddCut("", ' + composition.workAreaStart + ', ' + composition.workAreaDuration + ');\n';

		out += '\t\t}\n';
		out += '\n';
		return out;
	};
	var writeLayerAnimation = function (node, mode, workAreaStart, workAreaEnd) {
		var layer = node.layer;
		var out = '';
		var keys;
		if (layer.position && layer.position.keys) {
			keys = layer.position.keys;
			if (!(keys.length)) {
				alert("composition.layers[i].position.keys is not array.");
				return null;
			}
			out += '\t\t\t\t.AddPosition(\n';
			out += '\t\t\t\t\t"' + layer.name + '_position",\n';
			out += writeKeyTimes(keys);
			out += writeKeyValues(keys, 0);
			out += writeKeyValues(keys, 1, true);
		}
		if (layer.scale && layer.scale.keys) {
			keys = layer.scale.keys;
			if (!(keys.length)) {
				alert("composition.layers[i].scale.keys is not array.");
				return null;
			}
			out += '\t\t\t\t.AddScale(\n';
			out += '\t\t\t\t\t"' + layer.name + '_scale",\n';
			out += writeKeyTimes(keys);
			if (isKeyValueXYSame(keys) === false) {
				out += writeKeyValues(keys, 0);
			}
			out += writeKeyValues(keys, 1, true);
		}
		if (layer.rotation && layer.rotation.keys) {
			keys = layer.rotation.keys;
			if (!(keys.length)) {
				alert("composition.layers[i].rotation.keys is not array.");
				return null;
			}
			out += '\t\t\t\t.AddRotation(\n';
			out += '\t\t\t\t\t"' + layer.name + '_rotation",\n';
			out += writeKeyTimes(keys);
			out += writeKeyValues(keys, 0, true);
		}
		if (layer.opacity && layer.opacity.keys) {
			keys = layer.opacity.keys;
			if (!(keys.length)) {
				alert("composition.layers[i].opacity.keys is not array.");
				return null;
			}
			out += '\t\t\t\t.AddOpacity(\n';
			out += '\t\t\t\t\t"' + layer.name + '_opacity",\n';
			out += writeKeyTimes(keys);
			out += writeKeyValues(keys, 0, true);
		}
		var inPoint = layer.inPoint;
		var duration = layer.duration;
		var endFrame = inPoint + duration;
		if (inPoint < workAreaStart) {
			inPoint = workAreaStart;
		}
		if (endFrame > workAreaEnd) {
			endFrame = workAreaEnd;
		}
		// 全域入っていれば無用
		if ((inPoint > workAreaStart) || (endFrame < workAreaEnd)) {
			duration = endFrame - inPoint;
			out += '\t\t\t\t.AddVisibility("' + layer.name + '_visibility", ' + inPoint + ', ' + duration + ')\n';
		}
		// ヘッダコメントは中身がある場合に限って最後に足す
		if (out !== ''){
			var firstLine = '\t\t\t\t// ' + layer.name + '\n';
			out = firstLine + out;
		}
		return out;
	};
	var writeKeyTimes = function (keys) {
		var out = '';
		out += '\t\t\t\t\tnew int[] { ';
		for (var i = 0; i < (keys.length - 1); ++i) {
			out += keys[i].time + ', ';
		}
		out += keys[keys.length - 1].time + ' },\n';
		return out;
	};
	var isKeyValueXYSame = function (keys) {
		var ret = true;
		for (var i = 0; i < keys.length; ++i) {
			key = keys[i];
			if (key.value[0] !== key.value[1]) {
				ret = false;
				break;
			}
		}
		return ret;
	};
	var writeKeyValues = function (keys, index, isLastArg) {
		var out = '';
		out += '\t\t\t\t\tnew float[] { ';
		var key;
		for (var i = 0; i < (keys.length - 1); ++i) {
			key = keys[i];
			if (key.value.length) {
				out += key.value[index] + 'f, ';
			} else if (index === 0) {
				out += key.value + 'f, ';
			} else {
				alert("BUG");
				return null;
			}
		}
		key = keys[keys.length - 1];
		if (key.value.length) {
			out += key.value[index] + 'f }';
		} else if (index === 0) {
			out += key.value + 'f }';
		} else {
			alert("BUG");
			return null;
		}
		if (isLastArg) {
			out += ')\n';
		} else {
			out += ',\n';
		}
		return out;
	};
	var writeInitializeInstance = function (nodeArray, mode, workAreaStart, workAreaEnd) {
		var out = '';
		out += '\t\tprotected override void InitializeInstance(AfterEffectsInstance instance)\n'
		out += '\t\t{\n';

		// 先にレイヤーバインディングを書いて、使っているものと使っていないものを識別する
		var layerBindingText = '\t\t\tinstance\n';
		var layerUsedFlags = [];
		for (i = 0; i < nodeArray.length; ++i) {
			var text = writeLayerBinding(nodeArray[i], mode, workAreaStart, workAreaEnd);
			if (text !== null){
				layerUsedFlags[i] = true;
				layerBindingText += text;
			}else{
				layerUsedFlags[i] = false;
			}
		}
		layerBindingText += '\t\t\t;\n';

		// transform変数生成
		for (i = 0; i < nodeArray.length; ++i) {
			var layer = nodeArray[i].layer;
			if (layerUsedFlags[i]){
				if (mode === self.Mode.Ugui) {
					if (layer.isImage){
						out += '\t\t\tvar ' + layer.name + '_transform = _' + layer.name + '.gameObject.GetComponent<RectTransform>();\n';
					}
				} else if (mode === self.Mode.Unity2d) {
					out += '\t\t\tvar ' + layer.name + '_transform = _' + layer.name + '.gameObject.transform.parent;\n';
				} else {
					alert('BUG');
				}
			}
		}
		out += '\n';
		out += layerBindingText;
		out += '\t\t}\n';
		out += '\n';
		return out;
	};
	var writeLayerBinding = function (node, mode, workAreaStart, workAreaEnd) {
		var layer = node.layer;
		var transformName = '_' + layer.name;
		if (layer.isImage){
			transformName = layer.name + '_transform';
		}
		var out = '';
		var keys;
		var exists = false;
		if (layer.position && layer.position.keys) {
			keys = layer.position.keys;
			if (!(keys.length)) {
				alert("composition.layers[i].position.keys is not array.");
				return null;
			}
			out += '\t\t\t\t.BindPosition(' + transformName + ', "' + layer.name + '_position")\n';
			exists = true;
		}
		if (layer.scale && layer.scale.keys) {
			keys = layer.scale.keys;
			if (!(keys.length)) {
				alert("composition.layers[i].scale.keys is not array.");
				return null;
			}
			out += '\t\t\t\t.BindScale(' + transformName + ', "' + layer.name + '_scale")\n';
			exists = true;
		}
		if (layer.rotation && layer.rotation.keys) {
			keys = layer.rotation.keys;
			if (!(keys.length)) {
				alert("composition.layers[i].rotation.keys is not array.");
				return null;
			}
			out += '\t\t\t\t.BindRotation(' + transformName + ', "' + layer.name + '_rotation")\n';
			exists = true;
		}
		if (layer.opacity && layer.opacity.keys) {
			keys = layer.opacity.keys;
			if (!(keys.length)) {
				alert("composition.layers[i].opacity.keys is not array.");
				return null;
			}
			if (layer.isImage){
				out += '\t\t\t\t.BindOpacity(_' + layer.name + ', "' + layer.name + '_opacity")\n';
			}else{
				out += '//\t\t\t\t.BindOpacity(_' + layer.name + ', "' + layer.name + '_opacity") // TODO: 適切に下流に流すコードを手動で書いてくれ。CanvasGroupをつけるのが良いと思う。\n';
			}
			exists = true;
		}
		var inPoint = layer.inPoint;
		var duration = layer.duration;
		var endFrame = inPoint + duration;
		if (inPoint < workAreaStart) {
			inPoint = workAreaStart;
		}
		if (endFrame > workAreaEnd) {
			endFrame = workAreaEnd;
		}
		if ((inPoint > workAreaStart) || (endFrame < workAreaEnd)) {
			if (layer.isImage){
				out += '\t\t\t\t.BindVisibility(_' + layer.name + ', "' + layer.name + '_visibility")\n';
			}else{
				out += '\t\t\t\t.BindVisibility(_' + layer.name + '.gameObject, "' + layer.name + '_visibility")\n';
			}
			exists = true;
		}
		if (exists){
			var firstLine = '\t\t\t\t// ' + layer.name + '\n';
			out = firstLine + out;
		}else{
			out = null;
		}
		return out;
	};
	var writeResourceFunctions = function () {
		var out = '';
		out += '\t\tpublic static void DestroyResource()\n';
		out += '\t\t{\n';
		out += '\t\t\t_resource = null;\n';
		out += '\t\t}\n';
		out += '\n';

		out += '\t\tprotected override AfterEffectsResource GetResource()\n';
		out += '\t\t{\n';
		out += '\t\t\tif (_resource == null)\n';
		out += '\t\t\t{\n';
		out += '\t\t\t\tCreateResource();\n';
		out += '\t\t\t}\n';
		out += '\t\t\treturn _resource;\n';
		out += '\t\t}\n';
		out += '\n';
		return out;
	}
	// Layer名が.を含んだ場合、_に置換する。
	var modifyLayerNames = function (layers) {
		if (layers) {
			for (var i = 0; i < layers.length; ++i) {
				var layer = layers[i];
				layer.name = 'layer' + layer.index + '_' + layer.name.replace(/[\.\s\/\!\-\:]/g, '_');
			}
		}
	};
	//layer間の親子関係を抽出して木を作る。ついでにそこに諸々の情報を加味して後段で使う。nodeArrayは加味されたlayersと同じ並びの配列
	var extractTree = function (nodeArray, layers) {
		// まず全レイヤを根の子にしつつ、レイヤ名→オブジェクトの辞書を作る
		var root = createTreeNode(null);
		var map = {};
		var i;
		for (i = 0; i < layers.length; ++i) {
			var layer = layers[i];
			var node = createTreeNode(layer);
			nodeArray.push(node);
			map[node.layer.index] = node;
			addChild(root, node);
		}
		// 各レイヤについて親を検索し、親オブジェクトの子につなぎなおす。
		var child = root.childHead.next;
		while (child !== root.childTail) {
			var next = child.next;
			if (typeof child.layer.parentIndex !== 'undefined') {
				var parent = map[child.layer.parentIndex];
				if (parent) {
					addChild(parent, child);
				}
			}
			child = next;
		}
		return root;
	};
	//childHeadとchildTailは番兵でダミー。
	var createTreeNode = function (aeLayer) {
		var node = {
			layer: aeLayer,
			prev: null,
			next: null,
			parent: null,
			childHead: { prev: null },
			childTail: { next: null }
		};
		node.childHead.next = node.childTail;
		node.childTail.prev = node.childHead;
		if (aeLayer) {
			extractFirstFrameLayerInfo(node, aeLayer);
		}
		return node;
	};
	var extractFirstFrameLayerInfo = function (dst, aeLayer) {
		dst.name = aeLayer.name;
		var anchor = extractFirstFrameValue(aeLayer.anchorPoint, [0.0, 0.0, 0.0]);
		dst.anchorX = anchor[0];
		dst.anchorY = anchor[1];

		var position = extractFirstFrameValue(aeLayer.position, [0.0, 0.0, 0.0]);
		dst.positionX = position[0];
		dst.positionY = position[1];

		var scale = extractFirstFrameValue(aeLayer.scale, [100.0, 100.0, 100.0]);
		dst.scaleX = scale[0];
		dst.scaleY = scale[1];

		dst.rotation = extractFirstFrameValue(aeLayer.rotation, 0.0);
		dst.opacity = extractFirstFrameValue(aeLayer.opacity, 100.0);
	};
	var extractFirstFrameValue = function (property, defaultValue) {
		var ret = defaultValue;
		if (property) {
			if (property.value) {
				ret = property.value;
			} else if (property.keys && property.keys.length) {
				if (property.keys[0] && property.keys[0].value) {
					ret = property.keys[0].value;
				}
			}
		}
		return ret;
	};
	// 末尾追加
	var addChild = function (parent, child) {
		// 切り離し
		var prev = child.prev;
		var next = child.next;
		child.prev = null;
		child.next = null;
		if (prev) {
			prev.next = next;
		}
		if (next) {
			next.prev = prev;
		}
		// 追加
		prev = parent.childTail.prev;
		next = parent.childTail;
		prev.next = child;
		child.prev = prev;
		child.next = next;
		next.prev = child;
		child.parent = parent;
	};
	var writeCompositionHeader = function (composition, treeRoot, forUgui) {
		if (!composition) {
			alert("composition null or undefined.");
			return null;
		}
		var out = '';
		out += '// generated from composition ' + composition.name + '\n';

		// 木を表示
		out += '\n';
		out += '// [object hierarchy]\n';
		out += writeTreeGraph(treeRoot);
		out += '\n';

		out += 'using UnityEngine;\n';
		if (forUgui) {
			out += 'using UnityEngine.UI;\n';
		}
		out += '\n';
		out += 'namespace ' + self.OutputNamespace + '\n';
		out += '{\n';
		if (!(composition.name)) {
			alert("composition has no name.");
			return null;
		}
		out += '\tpublic class ' + composition.name + ' : AfterEffectsAnimation\n';
		out += '\t{\n';
		// リソース
		out += '\t\tprivate static AfterEffectsResource _resource;\n';

		// スプライトメンバ追加
		for (var i = 0; i < composition.layers.length; ++i) {
			var layer = composition.layers[i];
			if (layer.isImage){
				out += '\t\t[SerializeField]\n';
				out += '\t\tprivate Sprite _' + layer.name + '_sprite;\n';
			}
		}
		out += '\n';
		out += '\t\t[Header("以下はBuildHierarchyで自動で入る")]\n';
		return out;
	};
	var writeTreeGraph = function (node, indent) {
		indent = indent || 0;
		var out = '';
		out += '// ';
		var i = 0;
		for (i = 0; i < indent; ++i) {
			out += '\t';
		}
		if (node.layer) {
			out += node.layer.name + '\n';
		} else {
			out += 'root\n';
		}
		var child = node.childHead.next;
		while (child !== node.childTail) {
			out += writeTreeGraph(child, indent + 1);
			child = child.next;
		}
		return out;
	};
	var alert = function (string) {
		if (Window && Window.alert && (typeof Window.alert === "function")) {
			Window.alert(string);
		} else if (window && window.alert && (typeof window.alert === "function")) {
			window.alert(string);
		}
	};
	var writeBuildHierarchy = function (treeRoot, composition, mode) {
		var out = '';
		// ヒエラルキー生成関数
		out += '#if UNITY_EDITOR\n';
		out += '\t\tprotected override void BuildHierarchy()\n';
		out += '\t\t{\n';
		out += '\t\t\tgameObject.name = this.GetType().Name;\n';
		if (mode === self.Mode.Ugui) {
			out += '\t\t\tvar transform = gameObject.GetComponent<RectTransform>();\n';
			out += '\t\t\ttransform.sizeDelta = new Vector2(' +
				composition.width +
				'f, ' +
				composition.height +
				'f);\n';
		}
		out += '\t\t\tGameObject layerObj;\n';
		if (mode === self.Mode.Ugui) {
			out += '\t\t\tImage image;\n';
		} else if (mode === self.Mode.Unity2d) {
			out += '\t\t\tGameObject layerParentObj;\n';
			out += '\t\t\tSpriteRenderer renderer;\n';
		}
		out += '\n';
		if (mode === self.Mode.Ugui) {
			out += writeBuildHierarchyUgui(treeRoot, 'gameObject');
		} else if (mode === self.Mode.Unity2d) {
			out += writeBuildHierarchyUnity2d(treeRoot, 'gameObject');
		}
		out += '\t\t}\n';
		out += '#endif\n';
		out += '\t}\n';
		out += '}\n';
		out += '\n';
		return out;
	};
	var writeBuildHierarchyUgui = function (node, objectName) {
		out = '';
		// 逆順に足せば同階層での親子関係は正しくなる
		var child = node.childTail.prev;
		var layerObjName;
		while (child !== node.childHead) {
			layerObjName = child.layer.name;
			// gameObject生成。Imageを持った状態で作ることに注意。最初からRectTransformにしておく
			if (child.layer.isImage){
				out += '\t\t\tlayerObj = new GameObject("' + layerObjName + '", typeof(Image));\n';
				out += '\t\t\timage = layerObj.GetComponent<Image>();\n'
				out += '\t\t\timage.sprite = ' + layerObjName + '_sprite;\n';
				out += '\t\t\timage.raycastTarget = false;\n'; // レイキャストはするものに明示的につける方が楽だろう
				out += '\t\t\t_' + layerObjName + ' = image;\n';
			}else{
				out += '\t\t\tlayerObj = new GameObject("' + layerObjName + '", typeof(RectTransform));\n';
				out += '\t\t\t_' + layerObjName + ' = layerObj.GetComponent<RectTransform>();\n';
			}
			// 親は現オブジェクト
			out += '\t\t\tlayerObj.transform.SetParent(' + objectName + '.transform, false);\n';
			// サイズ設定
			out += '\t\t\tlayerObj.GetComponent<RectTransform>().sizeDelta = new Vector2(' + child.layer.width + ', ' + child.layer.height + ');\n';
			out += '\n';
			// 子に再帰
			out += writeBuildHierarchyUgui(child, '_' + layerObjName);
			child = child.prev;
		}
		return out;
	};
	var writeBuildHierarchyUnity2d = function (node, objectName) {
		out = '';
		var child = node.childHead.next;
		var layerObjName;
		var layerParentObjName;
		while (child !== node.childTail) {
			layerObjName = child.layer.name;
			layerParentObjName = layerObjName + '_root';
			// 親側を生成
			out += '\t\t\tlayerParentObj = new GameObject("' + layerParentObjName + '");\n';
			// 親の親はobjectName.transform
			out += '\t\t\tlayerParentObj.transform.SetParent(' + objectName + '.transform, false);\n'
			// 子を生成
			out += '\t\t\tlayerObj = new GameObject("' + layerObjName + '", typeof(SpriteRenderer));\n';
			// 子の親は親.transform
			out += '\t\t\tlayerObj.transform.SetParent(layerParentObj.transform);\n';
			// 子にAddComponentしてスプライト差し込み
			out += '\t\t\trenderer = layerObj.GetComponent<SpriteRenderer>();\n'
			out += '\t\t\trenderer.sprite = _' + layerObjName + '_sprite;\n';
			out += '\t\t\t_' + layerObjName + ' = renderer;\n';
			out += '\n';
			// 子に再帰
			out += writeBuildHierarchyUnity2d(child, '_' + layerObjName);
			child = child.next;
		}
		return out;
	};
}());


// HTMLでのエントリポイント関数
var htmlMain = function () {
	var Ae2u = AfterEffectsToUnity;
	var readFile = function (onLoad) {
		var files = document.getElementById('inputFile').files;
		if (files.length > 0) {
			var reader = new FileReader();
			reader.onload = function (e) {
				onLoad(e.target.result);
			};
			reader.readAsText(files[0]);
		}
	};
	var getMode = function () {
		var index = document.getElementById('mode').selectedIndex;
		var ret = Ae2u.Mode.Unknown;
		switch (index) {
			case 0: ret = Ae2u.Mode.Ugui; break;
			case 1: ret = Ae2u.Mode.Unity2d; break;
		}
		return ret;
	};
	var onConvertButtonClick = function () {
		readFile(function (inputText) {
			var mode = getMode();
			var project = JSON.parse(inputText);
			var outputData = Ae2u.convert(project, mode);
			var blob = new Blob([outputData], { type: 'text/plain' });
			var url = window.URL.createObjectURL(blob);
			var anchor = document.getElementById('saveOutputFile');
			anchor.download = 'converted.txt';
			anchor.href = url;
		});
	};
	document.getElementById('convertButton').addEventListener('click', onConvertButtonClick, false);
};

// エントリポイント
// HTML5であれば実行。そうでなければ、これをライブラリとして用いるであろうら、何もしない。
if (
	(typeof window === 'object') &&
	(typeof document === 'object')) {
	htmlMain();
}