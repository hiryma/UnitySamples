float calcFogExp(float3 objectPosition, float3 cameraPosition, float density)
{
	float3 camToObj = objectPosition - cameraPosition;
	float l = length(camToObj);
	float fog = exp(-l * density);
	return fog;
}

float calcFogHeightExp(float3 objectPosition, float3 cameraPosition, float densityY0, float densityAttenuation)
{
	float3 camToObj = cameraPosition - objectPosition;
	float l = length(camToObj);
	float ret;
	float tmp = l * densityY0 * exp(-densityAttenuation * objectPosition.y);
	if (camToObj.y == 0.0) // 単純な均一フォグ
	{
		ret = exp(-tmp);
	}
	else
	{
		float kvy = densityAttenuation * camToObj.y;
		ret = exp(tmp / kvy * (exp(-kvy) - 1.0));
	}
	return ret;
}

float calcFogHeightUniform(float3 objectPosition, float3 cameraPosition, float fogDensity, float fogEndHeight)
{
	float3 camToObj = cameraPosition - objectPosition;
	float t;
	if (objectPosition.y < fogEndHeight) // 物が霧の中にある
	{
		if (cameraPosition.y > fogEndHeight) // カメラは霧の外にある
		{
			t = (fogEndHeight - objectPosition.y) / camToObj.y;
		}
		else // カメラも霧の中にある
		{
			t = 1.0;
		}
	}
	else // 物が霧の外にいる
	{
		if (cameraPosition.y < fogEndHeight) // カメラは霧の中にいる
		{
			t = (cameraPosition.y - fogEndHeight) / camToObj.y;
		}
		else // カメラも霧の外にいる
		{
			t = 0.0;
		}
	}
	float distance = length(camToObj) * t;
	float fog = exp(-distance * fogDensity);
	return fog;
}

