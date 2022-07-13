using COM3D2.EditModeItemManager;
using UnityEngine;

namespace COM3D2.EditModeFavorites;

internal class FavoriteFrame : IOverlay {
	private static readonly Vector3 _largeButtonOffset = new(-20, -20);
	private static readonly Color _partialColor = new(1, 0.75f, 1);

	private static readonly Sprite _sprite;

	private readonly GameObject _gameObject;
	private readonly UI2DSprite _textureSprite;

	static FavoriteFrame() {
		var texture = Resources.Load<Texture2D>("Prefab/Particle/Textures/P_star");
		_sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), default);
	}

	public FavoriteFrame(OverlayContainer frameOverlayManager, bool useOffset) {
		_gameObject = NGUITools.AddChild(frameOverlayManager.Parent);
		_gameObject.name = "Favorite";
		_gameObject.transform.localPosition = new Vector3(frameOverlayManager.Anchor.width / 2, frameOverlayManager.Anchor.height / 2) + (useOffset ? _largeButtonOffset : default);

		_textureSprite = CreateSprite(_gameObject, "Texture");
		_textureSprite.depth = 12;

		var shadowSprite = CreateSprite(_gameObject, "Shadow");
		shadowSprite.color = Color.black;
		shadowSprite.depth = 11;
	}

	public bool Active {
		get => _gameObject.activeSelf;
		set => _gameObject.SetActive(value);
	}

	public bool Partial {
		set => _textureSprite.color = value ? _partialColor : Color.white;
	}

	private static UI2DSprite CreateSprite(GameObject parent, string name) {
		var frame = NGUITools.AddChild(parent);
		frame.name = name;

		var sprite = frame.AddComponent<UI2DSprite>();
		sprite.width = 32;
		sprite.height = 32;
		sprite.sprite2D = _sprite;
		return sprite;
	}
}
