#include "AfterEffectsToUnityCodeConverter.js"

// AfterEffects内で情報を吐き出す機能
var AfterEffectsDumper = AfterEffectsDumper || {};
(function initializeAfterEffectsDumper() {
	var self = AfterEffectsDumper;
	self.dumpAll = false;

	// 公開インターフェイス
	self.dump = function (dumpAll) {
		self.dumpAll = dumpAll;
		return parseProject(app.project);
	};

	// 非公開インターフェイス
	var convertValueType = function (aeType) {
		var ret = null;
		switch (aeType) {
			case PropertyValueType.NO_VALUE: ret = 'NO_VALUE'; break;
			case PropertyValueType.ThreeD_SPATIAL: ret = 'ThreeD_SPATIAL'; break;
			case PropertyValueType.ThreeD: ret = 'ThreeD'; break;
			case PropertyValueType.TwoD_SPATIAL: ret = 'TwoD_SPATIAL'; break;
			case PropertyValueType.TwoD: ret = 'TwoD'; break;
			case PropertyValueType.OneD: ret = 'OneD'; break;
			case PropertyValueType.COLOR: ret = 'COLOR'; break;
			case PropertyValueType.CUSTOM_VALUE: ret = 'CUSTOM_VALUE'; break;
			case PropertyValueType.MARKER: ret = 'MARKER'; break;
			case PropertyValueType.LAYER_INDEX: ret = 'LAYER_INDEX'; break;
			case PropertyValueType.MASK_INDEX: ret = 'MASK_INDEX'; break;
			case PropertyValueType.SHAPE: ret = 'SHAPE'; break;
			case PropertyValueType.TEXT_DOCUMENT: ret = 'TEXT_DOCUMENT'; break;
			default: assert(false); break;
		}
		return ret;
	};
	var convertInterpolationType = function (aeType) {
		var ret = null;
		switch (aeType) {
			case KeyframeInterpolationType.LINEAR: ret = 'LINEAR'; break;
			case KeyframeInterpolationType.BEZIER: ret = 'BEZIER'; break;
			case KeyframeInterpolationType.HOLD: ret = 'HOLD'; break;
			default: assert(false); break;
		}
		return ret;
	};
	var convertBlendingMode = function (aeType) {
		var ret = null;
		switch (aeType) {
			case BlendingMode.ADD: ret = 'ADD'; break;
			case BlendingMode.ALPHA_ADD: ret = 'ALPHA_ADD'; break;
			case BlendingMode.CLASSIC_COLOR_BURN: ret = 'CLASSIC_COLOR_BURN'; break;
			case BlendingMode.CLASSIC_COLOR_DODGE: ret = 'CLASSIC_COLOR_DODGE'; break;
			case BlendingMode.CLASSIC_DIFFERENCE: ret = 'CLASSIC_DIFFERENCE'; break;
			case BlendingMode.COLOR: ret = 'COLOR'; break;
			case BlendingMode.COLOR_BURN: ret = 'COLOR_BURN'; break;
			case BlendingMode.COLOR_DODGE: ret = 'COLOR_DODGE'; break;
			case BlendingMode.DANCING_DISSOLVE: ret = 'DANCING_DISSOLVE'; break;
			case BlendingMode.DIFFERENCE: ret = 'DIFFERENCE'; break;
			case BlendingMode.DISSOLVE: ret = 'DISSOLVE'; break;
			case BlendingMode.EXCLUSION: ret = 'EXCLUSION'; break;
			case BlendingMode.HARD_LIGHT: ret = 'HARD_LIGHT'; break;
			case BlendingMode.HARD_MIX: ret = 'HARD_MIX'; break;
			case BlendingMode.HUE: ret = 'HUE'; break;
			case BlendingMode.LIGHTEN: ret = 'LIGHTEN'; break;
			case BlendingMode.LIGHTER_COLOR: ret = 'LIGHTER_COLOR'; break;
			case BlendingMode.LINEAR_BURN: ret = 'LINEAR_BURN'; break;
			case BlendingMode.LINEAR_DODGE: ret = 'LINEAR_DODGE'; break;
			case BlendingMode.LINEAR_LIGHT: ret = 'LINEAR_LIGHT'; break;
			case BlendingMode.LUMINESCENT_PREMUL: ret = 'LUMINESCENT_PREMUL'; break;
			case BlendingMode.LUMINOSITY: ret = 'LUMINOSITY'; break;
			case BlendingMode.MULTIPLY: ret = 'MULTIPLY'; break;
			case BlendingMode.NORMAL: ret = 'NORMAL'; break;
			case BlendingMode.OVERLAY: ret = 'OVERLAY'; break;
			case BlendingMode.PIN_LIGHT: ret = 'PIN_LIGHT'; break;
			case BlendingMode.SATURATION: ret = 'SATURATION'; break;
			case BlendingMode.SCREEN: ret = 'SCREEN'; break;
			case BlendingMode.SILHOUETE_ALPHA: ret = 'SILHOUETE_ALPHA'; break; //これPDFのスペルミスじゃね?
			case BlendingMode.SILHOUETTE_LUMA: ret = 'SILHOUETTE_LUMA'; break;
			case BlendingMode.SOFT_LIGHT: ret = 'SOFT_LIGHT'; break;
			case BlendingMode.STENCIL_ALPHA: ret = 'STENCIL_ALPHA'; break;
			case BlendingMode.STENCIL_LUMA: ret = 'STENCIL_LUMA'; break;
			case BlendingMode.VIVID_LIGHT: ret = 'VIVID_LIGHT'; break;
			default: assert(false); break;
		}
		return ret;
	};
	var parseKey = function (aeProperty, i, frameRate) {
		var ret = {};
		var inInterp = aeProperty.keyInInterpolationType(i);
		var outInterp = aeProperty.keyOutInterpolationType(i);
		if (inInterp === outInterp) {
			ret.interpolation = convertInterpolationType(inInterp);
		} else {
			ret.inInterpolation = convertInterpolationType(inInterp);
			ret.outInterpolation = convertInterpolationType(outInterp);
		}
		/*
			if (property.isSpatial){
				ret.inSpatialTangent = property.keyInSpatialTangent(i);
				ret.outSpatialTangent = property.keyOutSpatialTangent(i);
				ret.spatialAutoBezier = property.keySpatialAutoBezier(i);
				ret.spatialContinuous = property.keySpatialContinuous(i);
			}
			ret.inTemporalEase = property.keyInTemporalEase(i);
			ret.outTemporalEase = property.keyOutTemporalEase(i);
		//	ret.roving = property.keyRoving(i);
		*/
		if (aeProperty.keyTemporalAutoBezier(i)) {
			ret.temporalAutoBezier = true;
		}
		if (aeProperty.keyTemporalContinuous(i)) {
			ret.temporalContinuous = true;
		}
		var t = aeProperty.keyTime(i) * frameRate;
		ret.time = parseInt(t + 0.5);
		ret.value = aeProperty.keyValue(i);
		return ret;
	};
	var parseProperty = function (dst, aeProperty, frameRate) {
		dst.name = aeProperty.name;
		if (aeProperty.propertyValueType === PropertyValueType.NO_VALUE)
		{
			return;
		}
		//	dst.valueType = convertValueType(aeProperty.propertyValueType);
		//	dst.canVaryOverTime = aeProperty.canVaryOverTime;
		//	dst.isTimeVarying = aeProperty.isTimeVarying;
		// valueは現在時刻の値を返す
		if (aeProperty.isTimeVarying === false) {
			dst.value = aeProperty.value;
		}
		//	dst.unitsText = aeProperty.unitsText;
		if (aeProperty.expressionEnabled) {
			dst.expressionEnabled = true;
			dst.expression = aeProperty.expression;
		}
		if (aeProperty.numKeys > 0) {
			//		dst.numKeys = aeProperty.numKeys;
			dst.keys = [];
			var dstIndex = 0;
			for (var i = 1; i <= aeProperty.numKeys; ++i) {
				dst.keys[dstIndex] = parseKey(aeProperty, i, frameRate);
				++dstIndex;
			}
			// 全部のキーで補間タイプが同じならキーから消してプロパティで持つ
			if ((dstIndex > 0) && (typeof dst.keys[0].interpolation !== "undefined")) {
				var interpolation = dst.keys[0].interpolation;
				var allSame = true;
				for (var i = 1; i < dst.keys.length; ++i) {
					if (dst.keys[i].interpolation !== interpolation) {
						allSame = false;
						break;
					}
				}
				if (allSame) {
					for (var i = 0; i < dst.keys.length; ++i) {
						delete dst.keys[i].interpolation;
					}
					dst.interpolation = interpolation;
				}
			}
			// キーが一個しかなく時刻0で、valueが存在しなければ定常値に置換する
			if ((typeof dst.value === 'undefined') && (dst.keys.length === 1) && (dst.keys[0].time === 0)) {
				dst.FROM_KEY = true;
				dst.value = dst.keys[0].value;
				delete dst.keys;
				if (typeof dst.interpolation !== 'undefined') {
					delete dst.interpolation;
				}
			}
		}
	};
	var parsePropertyGroup = function (dst, aePropertyGroup, frameRate) {
		dst.properties = [];
		for (var i = 0; i < aePropertyGroup.numProperties; ++i) {
			var aePropertyBase = aePropertyGroup.property(i + 1);
			dst.properties[i] = parsePropertyBase(aePropertyBase, frameRate);
		}
	};
	var parsePropertyBase = function (aePropertyBase, frameRate) {
		var ret = {};
		ret.name = aePropertyBase.name;
		ret.matchName = aePropertyBase.matchName;
		ret.propertyIndex = aePropertyBase.propertyIndex;
		ret.propertyDepth = aePropertyBase.propertyDepth;
		ret.propertyType = aePropertyBase.propertyType;
		ret.enabled = aePropertyBase.enabled;
		ret.active = aePropertyBase.active;
		ret.isEffect = aePropertyBase.isEffect;
		ret.isMask = aePropertyBase.isMask;
		if (aePropertyBase instanceof PropertyGroup){
			parsePropertyGroup(ret, aePropertyBase, frameRate);
		}else if (aePropertyBase instanceof Property){
			parseProperty(ret, aePropertyBase, frameRate);
		}
		return ret;
	};
	var parseTextLayer = function (dst, aeTextLayer, frameRate) {
		dst.sourceText = parsePropertyBase(aeTextLayer.property('sourceText'), frameRate);
	};
	var parseShapeLayer = function (dst, aeShapeLayer, frameRate) {
		dst.shapeProperties = [];
//		var n = aeShapeLayer.numProperties;
		var group = aeShapeLayer.property('ADBE Root Vectors Group');
		var n = group.numProperties;
		for (var i = 0; i < n; ++i){
//			dst.shapeProperties[i] = parsePropertyBase(aeShapeLayer.property(i + 1));
			dst.shapeProperties[i] = parsePropertyBase(group.property(i + 1));
		}
	};
	var parseAVLayer = function (dst, aeAVLayer, frameRate) {
		if (aeAVLayer.source !== null) {
//			dst.source = aeAVLayer.source.name;
			dst.name = aeAVLayer.source.name; // この方が使いやすい気がする
		}
		dst.width = aeAVLayer.width;
		dst.height = aeAVLayer.height;
		dst.blendingMode = convertBlendingMode(aeAVLayer.blendingMode);
		//	dst.effectsActive = aeAVLayer.effectsActive;
		dst.anchorPoint = parsePropertyBase(aeAVLayer.property('anchorPoint'), frameRate);
		dst.position = parsePropertyBase(aeAVLayer.property('position'), frameRate);
		dst.scale = parsePropertyBase(aeAVLayer.property('scale'), frameRate);
		dst.rotation = parsePropertyBase(aeAVLayer.property('rotation'), frameRate);
		//	dst.rotationZ = parseProperty(aeAVLayer.property('rotationZ'), frameRate); //よくわからない。
		dst.opacity = parsePropertyBase(aeAVLayer.property('opacity'), frameRate);
		dst.mask = parsePropertyBase(aeAVLayer.property('Masks'));
		//	dst.marker = parseProperty(aeAVLayer.property('marker'), frameRate);
		if (aeAVLayer instanceof TextLayer) {
			parseTextLayer(dst, aeAVLayer, frameRate);
		} else if (aeAVLayer instanceof ShapeLayer) {
			parseShapeLayer(dst, aeAVLayer, frameRate);
		}
	};
	var parseLayer = function (aeLayer, frameRate) {
		if ((self.dumpAll == false) && (aeLayer.enabled === false)) {
			return null;
		}
		var ret = {};
		ret.enabled = aeLayer.enabled;
		ret.index = aeLayer.index;
		//	ret.locked = aeLayer.locked;
		ret.name = aeLayer.name;
		//	ret.nullLayer = aeLayer.nullLayer;
		if (aeLayer.parent !== null) {
			ret.parentIndex = aeLayer.parent.index;
		}
		//	ret.solo = aeLayer.solo;
		//	ret.startTime = aeLayer.startTime * frameRate; // これ意味がわからない
		ret.inPoint = parseInt((aeLayer.inPoint * frameRate) + 0.5);
		// outPointは「表示される最後のフレーム」なので扱いづらい。画面上にはdurationがあるのでそれを出すとする。
		// しかし、ここで出てくるoutPointはなぜか「表示されなくなる最初のフレーム」で、AE上のoutPointの次のフレームの時刻になっている!!
		var outPoint = parseInt((aeLayer.outPoint * frameRate) + 0.5);
		ret.duration = outPoint - ret.inPoint;
		//	ret.stretch = aeLayer.stretch;
		if ((aeLayer instanceof AVLayer) || (aeLayer instanceof TextLayer) || (aeLayer instanceof ShapeLayer)) {
			parseAVLayer(ret, aeLayer, frameRate);
		}
		return ret;
	};
	var parseCompItem = function (aeCompItem) {
		var ret = parseItem(aeCompItem);
		ret.frameRate = aeCompItem.frameRate;
		ret.workAreaStart = parseInt((aeCompItem.workAreaStart * ret.frameRate) + 0.5);
		// プログラマ的にはEndの方がいいのだが、Endを含むのか含まないのか?的なことが問題にならないようにするには間隔の方がいい
		ret.workAreaDuration = parseInt((aeCompItem.workAreaDuration * ret.frameRate) + 0.5);
		var layers = aeCompItem.layers;
		ret.layers = [];
		var dstIndex = 0;
		for (var i = 1; i <= layers.length; ++i) {
			var layer = parseLayer(layers[i], aeCompItem.frameRate);
			if (layer != null) {
				ret.layers[dstIndex] = layer;
				++dstIndex;
			}
		}
		return ret;
	};
	var parseFootageItem = function (aeFootageItem) {
		var ret = parseItem(aeFootageItem);
		var source = aeFootageItem.mainSource;
		if (source !== null) {
			if (source.hasAlpha) {
				ret.hasAlpha = true;
			}
			if (source.invertAlpha) {
				ret.invertAlpha = true;
			}
			//		ret.isStill = source.isStill;
			// 静止画ならdurationは不要
			if (source.isStill) {
				delete ret.duration;
			}
		}
		return ret;
	};
	var parseItem = function (aeItem) {
		var ret = {};
		ret.name = aeItem.name;
		ret.width = aeItem.width;
		ret.height = aeItem.height;
		ret.duration = aeItem.duration;
		return ret;
	};
	var parseProject = function (aeProject) {
		var ret = {};
		if (aeProject.file !== null) {
			ret.name = aeProject.file.displayName;
		} else {
			ret.name = "untitled";
		}

		ret.compositions = [];
		ret.footages = [];
		var items = aeProject.items;
		var compositionIndex = 0;
		var footageIndex = 0;
		for (var i = 1; i <= items.length; ++i) {
			var item = items[i];
			if (item instanceof CompItem) {
				ret.compositions[compositionIndex] = parseCompItem(item);
				++compositionIndex;
			} else if (item instanceof FootageItem) {
				ret.footages[footageIndex] = parseFootageItem(item);
				++footageIndex;
			}
		}
		return ret;
	};
}());

// AfterEffectsでのエントリポイント関数
(function afterEffectsMain() {
	// UI表示
	var dlg = new Window('dialog', '出力モードを選んで「実行」を押してね');
	dlg.modePanel = dlg.add('panel', undefined, 'Mode');
	var forUguiButton = dlg.modePanel.add('radiobutton', undefined, 'UGUI');
	var forUnity2dButton = dlg.modePanel.add('radiobutton', undefined, 'Unity2D');
	forUguiButton.value = true;

	dlg.typePanel = dlg.add('panel', undefined, 'FileType');
	var csButton = dlg.typePanel.add('radiobutton', undefined, 'Unity C# Code');
	var jsonButton = dlg.typePanel.add('radiobutton', undefined, 'JSON');
	var dumpAllCheckbox = dlg.typePanel.add('checkbox', undefined, '不可視でも吐く');
	csButton.value = true;
	dumpAllCheckbox.value = true;

	dlg.add('button', undefined, '実行', { name: 'ok' });
	dlg.add('button', undefined, 'キャンセル', { name: 'cancel' });
	dlg.center();
	var dlgRet = dlg.show();

	if (dlgRet === 1) {
		var project = AfterEffectsDumper.dump(dumpAllCheckbox.value);
		var outputData = null;
		var ext = null;
		if (csButton.value) {
			var ext = 'cs';
			var mode = AfterEffectsToUnity.Mode.Unknown;
			if (forUguiButton.value) {
				mode = AfterEffectsToUnity.Mode.Ugui;
			} else {
				mode = AfterEffectsToUnity.Mode.Unity2d;
			}
			outputData = AfterEffectsToUnity.convert(project, mode);
		} else {
			var ext = 'json';
			if (typeof JSON !== 'undefined') {
				outputData = JSON.stringify(project, null, 3);
			} else {
				alert('JSONがないよ!toSourceで代替するので人が読めないJSONが出ます!');
				outputData = project.toSource();
			}
		}
		// 以下保存
		var filename = '~/' + project.name + '.' + ext;
		var dummyFile = new File(filename);
		// File.saveDialogだとデフォルトファイル名を入れられない。saveDlgでも好きな名前が入れられるわけではないがマシ。
		var path = dummyFile.saveDlg("保存するファイルを選んでね!");
		if (path !== null) {
			var file = new File(path);
			file.encoding = 'UTF8';
			file.lineFeed = 'Unix';
			if (file.open('w')) {
				if (file.write(outputData)) {
				}
				else {
					Window.alert("ファイルに書き込めない");
				}
			}
			else {
				Window.alert("ファイルを開けない");
			}
			file.close();
		}
	}
}());
